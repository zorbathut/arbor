
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor
{
    public class TreeDec : Dec.Dec
    {
        public object worker;

        public interface ITreeFactory
        {
            Node Create(TreeDec treeDec);
        }

        [NonSerialized]
        internal Node root;

        [NonSerialized]
        internal Node[] nodes;

        // Serialization help functionality
        [NonSerialized] internal List<(Type type, BlackboardIdentifier id)> blackboardRegistrations = new();
        [NonSerialized] internal Dictionary<ulong, int> blackboardLocalIdLookup = new Dictionary<ulong, int>();
        [NonSerialized] internal ulong signature = 0;
        [NonSerialized] internal ulong signatureStructure = 0;

        [NonSerialized] internal Blackboard blackboardTemplate;

        public void BlackboardRegister<T>(BlackboardParameter<T> id)
        {
            if (id.identifier == null)
            {
                Dbg.Err("Attempted to register a blackboard parameter from a constant");
                return;
            }

            if (signature != 0)
            {
                Dbg.Err("Cannot add more blackboard parameters after the tree has been loaded");
                return;
            }

            // check if this is already registered
            if (blackboardLocalIdLookup.TryGetValue(id.identifier.Value.uid, out int existingIndex))
            {
                if (blackboardRegistrations[existingIndex].type != typeof(T) || blackboardRegistrations[existingIndex].id.label != id.identifier.Value.label)
                {
                    Dbg.Err($"Blackboard parameter `{id}` is already registered with a different type or label somehow");
                }
                return;
            }

            // add to our ordered list
            blackboardLocalIdLookup[id.identifier.Value.uid] = blackboardRegistrations.Count;
            blackboardRegistrations.Add((typeof(T), id.identifier.Value));

            blackboardTemplate.Register(id);
        }

        [Dec.Setup]
        private void ValidateWorker(Action<string> reporter)
        {
            if (worker == null)
            {
                reporter("Worker is null; at the moment this is mandatory");
                return;
            }

            if (!(worker is ITreeFactory))
            {
                reporter("Worker is not ITreeFactory; at the moment this is mandatory");
            }
        }

        [Dec.Setup]
        [Dec.SetupAfter(typeof(TreeDec), nameof(ValidateWorker))]
        private void BuildTree(Action<string> reporter)
        {
            // make our template
            blackboardTemplate = new Blackboard();

            // create the actual tree structure
            var nodeList = new List<Node>();
            try
            {
                Blackboard.writeOnly = true;
                {
                    // this is excruciatingly hacky
                    using var scope = new State.Scope(State.ForSetup(this));

                    root = (worker as ITreeFactory).Create(this);
                }

                // run all the initialization/registration code
                // right now this really does not support parallelism but that's OK
                Node.initRunning = true;
                root?.Init(this, nodeList);
            }
            finally
            {
                Node.initRunning = false;
                Blackboard.writeOnly = false;
            }

            // for the sake of a little extra long-term efficiency
            nodes = nodeList.ToArray();

            if (nodes.Distinct().Count() != nodes.Length)
            {
                reporter("Nodes used multiple times in tree, each node must have exactly one parent");
            }

            // do this manually (this is ugly!)
            HashSet<object> registeredNodes = new HashSet<object>();
            var arrayPath = new Dec.PathMember(new Dec.PathDec(typeof(TreeDec), DecName), "nodeList");
            for (int i = 0; i < nodes.Length; ++i)
            {
                Dec.Database.DecLookupRegisterCustom(nodes[i], new Dec.PathIndex(arrayPath, i));
                registeredNodes.Add(nodes[i]);
            }

            // on a second pass, ban node members
            foreach (var node in nodes)
            {
                // we also want to forbid any members of the node, since those shouldn't be serialized in general
                foreach (var field in node.GetType().GetFields(
                             System.Reflection.BindingFlags.Public |
                             System.Reflection.BindingFlags.NonPublic |
                             System.Reflection.BindingFlags.Instance))
                {
                    var value = field.GetValue(node);
                    if (value != null && Dec.Util.CanBeShared(value.GetType()) && !registeredNodes.Contains(value))
                    {
                        Dec.Database.DecRegisterForbid(value);
                    }
                }
            }

            // Two signatures, which State.Record compares against a save's to decide how much of it to trust.
            //
            // `signature` covers both the blackboard registrations and each node's Worker layout. A saved enumerator is recorded by its compiler-generated field names (`<zone>5__2`), which differ between build configurations and shift when a Worker's locals are edited; Dec would then resume it mid-flight with the unmatched locals left at default. Hashing those names lets State.Record notice and restart instead. Only the Worker's own state machine is covered: an edit that only adds or reorders `yield`s (renumbering `<>1__state` without renaming anything), or one inside a lambda or a helper iterator the Worker drives, goes unnoticed. Its formula is what older saves carry, so changing it restarts every tree in every old save.
            //
            // `signatureStructure` covers the registrations and the node types in order, leaving out the Worker layouts. It decides whether the saved blackboard still reads back position for position; when it matches but `signature` doesn't, the load keeps the blackboard and restarts only the nodes. It also catches what `signature` can't, a node swapped for a subclass that inherits the same Worker. It can't tell a parameter whose meaning changed under the same type and label, so a change of meaning wants a new label.
            //
            // The registrations go in as (type, label): the uid comes from a process-wide counter, so it would make the signature differ from run to run.
            var registrations = blackboardRegistrations.Select(registration => (registration.type, registration.id.label)).ToList();
            signature = Dec.Recorder.Checksum((registrations, nodes.Select(NodeLayout).ToList()));
            signatureStructure = Dec.Recorder.Checksum((registrations, nodes.Select(node => node.GetType().ToString()).ToList()));
        }

        // What Dec records for a node's Worker enumerator: the state machine's type, and each field by name and declared type. Read off a fresh enumerator, which a C# iterator hands out without running any of the Worker's body.
        internal static string NodeLayout(Node node)
        {
            // A Worker that isn't recordable can't be saved mid-flight in the first place, so its type is all there is to match.
            if (!node.GetType().GetMethod(nameof(Node.Worker), Type.EmptyTypes).IsDefined(typeof(Dec.RecorderEnumerator.RecordableEnumerableAttribute), false))
            {
                return node.GetType().ToString();
            }

            var root = Dec.Reflection.Enumerate(node.Worker().GetEnumerator());

            var builder = new System.Text.StringBuilder();
            builder.Append(root.Value.GetType()).Append('\n');
            foreach (var entry in root.Children)
            {
                builder.Append(entry.Label).Append(':').Append(entry.DeclaredType).Append('\n');
            }

            return builder.ToString();
        }
    }
}
