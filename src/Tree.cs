
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
        [NonSerialized] internal List<(Type type, string name)> blackboardRegistrations = new();
        [NonSerialized] internal List<BlackboardIdentifier> blackboardLocalId = new();
        [NonSerialized] internal Dictionary<ulong, int> blackboardLocalIdLookup = new Dictionary<ulong, int>();
        [NonSerialized] internal ulong blackboardSignature = 0;

        [NonSerialized] internal Blackboard blackboardTemplate;

        public void BlackboardRegister<T>(BlackboardParameter<T> id)
        {
            if (id.identifier == null)
            {
                Dbg.Err("Attempted to register a blackboard parameter from a constant");
                return;
            }

            if (blackboardSignature != 0)
            {
                Dbg.Err("Cannot add more blackboard parameters after the tree has been loaded");
                return;
            }

            // check if this is already registered
            if (blackboardLocalIdLookup.TryGetValue(id.identifier.Value.uid, out int existingIndex))
            {
                if (blackboardRegistrations[existingIndex].type != typeof(T) || blackboardRegistrations[existingIndex].name != id.identifier.Value.label)
                {
                    Dbg.Err($"Blackboard parameter `{id}` is already registered with a different type or label somehow");
                }
                return;
            }

            // add to our ordered list
            blackboardRegistrations.Add((typeof(T), id.identifier.Value.label));
            blackboardLocalIdLookup[id.identifier.Value.uid] = blackboardLocalId.Count;
            blackboardLocalId.Add(id.identifier.Value);

            blackboardTemplate.Register(id);
        }

        public override void ConfigErrors(Action<string> reporter)
        {
            base.ConfigErrors(reporter);

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

        public override void PostLoad(Action<string> reporter)
        {
            base.PostLoad(reporter);

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

            // register blackboard hash
            blackboardSignature = Dec.Recorder.Checksum(blackboardRegistrations);

            // for the sake of a little extra long-term efficiency
            nodes = nodeList.ToArray();

            if (nodes.Distinct().Count() != nodes.Length)
            {
                reporter("Nodes used multiple times in tree, each node must have exactly one parent");
            }

            // do this manually (this is ugly!)
            var arrayPath = new Dec.PathMember(new Dec.PathDec(typeof(TreeDec), DecName), "nodeList");
            for (int i = 0; i < nodes.Length; ++i)
            {
                Dec.Database.RegisterLookup(nodes[i], new Dec.PathIndex(arrayPath, i));
            }
        }
    }
}
