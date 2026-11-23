
using System.Collections.Generic;

namespace Arbor
{
    /// <summary>
    /// Debug visualization state for a node (separate from Result which is used for execution).
    /// </summary>
    public enum DebugNodeState { Working, Success, Failure, Terminated }

    public class State : Dec.IRecordable
    {
        public static System.Threading.ThreadLocal<State> Current = new();
        public struct Scope : System.IDisposable
        {
            private State old;
            private State current;
            public Scope(State state)
            {
                old = Current.Value;
                current = state;
                Current.Value = current;
            }

            public void Dispose()
            {
                Assert.AreSame(Current.Value, current);
                Current.Value = old;
            }
        }

        // the tree we refer to
        internal Arbor.TreeDec tree;
        public TreeDec Tree => tree;

        internal struct NodeEntry : Dec.IRecordable
        {
            public IEnumerator<Result> enumerator;

            // debug visualization: how the node last finished, and the debugCurrentFrame it finished on
            public DebugNodeState? debugState;
            public int debugFrame;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref enumerator, nameof(enumerator));
                recorder.Record(ref debugState, nameof(debugState));
                recorder.Record(ref debugFrame, nameof(debugFrame));
            }
        }

        // local state; nodeEntries is indexed by Node.nodeIndex
        internal NodeEntry[] nodeEntries;
        internal Blackboard blackboard;

        // refreshed on every update; used for event triggers
        internal List<int> active;

        // ephemeral, used for debugging
        internal List<Node> stack = new List<Node>();

        // debug visualization state (for now, serialized)
        internal int debugCurrentFrame;

        // How much of the saved state a load had to discard.
        public enum LoadReset
        {
            None,

            // The nodes started over, discarding the Workers that were running; the blackboard is the saved one.
            Nodes,

            // The whole tree started over: the blackboard is back to the template, missing whatever the host put there when it created this State.
            Full,
        }

        // Set by a load that discarded saved state. Either way the discarded Workers were never disposed, and after a Full reset the blackboard needs its host's values back, so the host has to recover the State before anything else sees it. Never recorded, since it must not outlive the load that set it; Record reports a State still carrying it.
        private LoadReset loadReset;

        private State() { } // needed for Dec record
        public State(TreeDec tree)
        {
            this.tree = tree;

            ResetToStart();
        }

        internal static State ForSetup(TreeDec tree)
        {
            State state = new State();
            state.tree = tree;
            state.blackboard = tree.blackboardTemplate; // crosslinked
            return state;
        }

        private void ResetToStart()
        {
            ResetNodesToStart();

            // get a copy of the initial blackboard
            blackboard = Dec.Recorder.Clone(tree.blackboardTemplate);
        }

        // Every node back to not having run, leaving the blackboard alone
        private void ResetNodesToStart()
        {
            nodeEntries = new NodeEntry[tree.nodes.Length];
            active = new List<int>();
            debugCurrentFrame = 0;
        }

        public void Update()
        {
            debugCurrentFrame++;
            active.Clear();
            using (new Scope(this))
            {
                tree.root?.Update();
            }
        }

        public void EventInvoke(EventDec ev)
        {
            EventInvokeWorker(ev, null);
        }

        public void EventInvoke<T1>(EventDec<T1> ev, T1 param1)
        {
            EventInvokeWorker(ev, new object[] { param1 });
        }

        public void EventInvoke<T1, T2>(EventDec<T1, T2> ev, T1 param1, T2 param2)
        {
            EventInvokeWorker(ev, new object[] { param1, param2 });
        }

        public void EventInvoke<T1, T2, T3>(EventDec<T1, T2, T3> ev, T1 param1, T2 param2, T3 param3)
        {
            EventInvokeWorker(ev, new object[] { param1, param2, param3 });
        }

        public void EventInvoke<T1, T2, T3, T4>(EventDec<T1, T2, T3, T4> ev, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            EventInvokeWorker(ev, new object[] { param1, param2, param3, param4 });
        }

        private void EventInvokeWorker(BaseEventDec ev, object[] param)
        {
            using (new Scope(this))
            {
                foreach (var index in active)
                {
                    if (index == -1)
                    {
                        continue;
                    }

                    if (tree.nodes[index].eventActions?.TryGetValue(ev, out var actions) ?? false)
                    {
                        foreach (var a in actions)
                        {
                            // SURE DO HOPE THE TYPES MATCH UP, EH
                            a.DynamicInvoke(param);
                        }
                    }
                }
            }
        }

        public T PropertyGet<T>(PropertyDec<T> prop)
        {
            // go through active nodes in reverse order
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var index = active[i];
                if (index == -1)
                {
                    continue;
                }

                if (tree.nodes[index].properties?.TryGetValue(prop, out var value) ?? false)
                {
                    return (T)value;
                }
            }

            return default;
        }

        public void Reset()
        {
            using (new Scope(this))
            {
                tree.root?.Reset();
            }
        }

        // How much of this State a load started over, if its host hasn't recovered it yet; clears it, so the caller takes on that job.
        public LoadReset LoadResetConsume()
        {
            var result = loadReset;
            loadReset = LoadReset.None;
            return result;
        }

        public Blackboard Blackboard()
        {
            return blackboard;
        }

        internal T BlackboardGet<T>(BlackboardParameter<T> identifier)
        {
            return blackboard.Get(identifier);
        }

        internal void BlackboardSet<T>(BlackboardParameter<T> identifier, T item)
        {
            blackboard.Set(identifier, item);
        }

        /// <summary>
        /// Get debug visualization state for a node.
        /// Returns the state and the number of frames since it was set (0 = this frame).
        /// </summary>
        public (DebugNodeState? state, int framesSince) DebugGetNodeState(Node node)
        {
            if (active.Contains(node.nodeIndex))
            {
                return (DebugNodeState.Working, 0);
            }
            var entry = nodeEntries[node.nodeIndex];
            return (entry.debugState, debugCurrentFrame - entry.debugFrame);
        }

        public void Record(Dec.Recorder recorder)
        {
            if (loadReset != LoadReset.None && recorder.Mode == Dec.Recorder.Direction.Write)
            {
                Dbg.Err($"{tree}: copied after being reset on load ({loadReset}) but before its host recovered it");

                // once per State is plenty; a held original would otherwise report on every later copy
                loadReset = LoadReset.None;
            }

            // this ends up being complicated
            if (recorder.Intent == Dec.Recorder.Purpose.Cloning)
            {
                // just copy it all over
                recorder.Record(ref tree, nameof(tree));
                recorder.Record(ref nodeEntries, nameof(nodeEntries));
                recorder.Record(ref blackboard, nameof(blackboard));
                recorder.Record(ref active, nameof(active));

                recorder.Record(ref debugCurrentFrame, nameof(debugCurrentFrame));
                return;
            }

            recorder.Record(ref tree, nameof(tree));

            bool restartNodes = false;
            if (recorder.Mode == Dec.Recorder.Direction.Write)
            {
                recorder.Record(ref tree.signature, "signature");
                recorder.Record(ref tree.signatureStructure, "signatureStructure");
            }
            else if (recorder.Mode == Dec.Recorder.Direction.Read)
            {
                ulong sig = 0;
                recorder.Record(ref sig, "signature");

                // absent from saves written before it existed, which then can't say what kind of change it was, so only a matching `signature` vouches for their structure
                ulong? sigStructure = null;
                recorder.Record(ref sigStructure, "signatureStructure");

                bool structureMatches = sigStructure.HasValue ? sigStructure == tree.signatureStructure : sig == tree.signature;
                if (!structureMatches)
                {
                    Dbg.Wrn($"{tree}: signature mismatch; this is likely due to a change in the tree structure, blackboard parameters, or Worker code, or a save from a different build configuration. Resetting.");

                    ResetDiscardingSaved(recorder);
                    return;
                }

                if (sig != tree.signature)
                {
                    Dbg.Wrn($"{tree}: signature mismatch in Worker code only; this is likely due to a change in a Worker's locals, or a save from a different build configuration. Restarting its nodes and keeping its blackboard.");
                    restartNodes = true;
                }
            }

            // the blackboard's record reads the tree's registrations through it
            using var scope = new Scope(this);

            if (restartNodes)
            {
                IgnoreSavedNodes(recorder);
                ResetNodesToStart();
                loadReset = LoadReset.Nodes;

                recorder.Record(ref blackboard, nameof(blackboard));
                return;
            }

            // Dec leaves a field alone when its element is absent, so clear it first: still null afterward means a save written before nodeEntries existed, whose per-node state the tree just starts over without. (Cloning returned above.)
            bool loading = recorder.Mode == Dec.Recorder.Direction.Read;
            if (loading)
            {
                nodeEntries = null;
            }
            recorder.Record(ref nodeEntries, nameof(nodeEntries));
            if (loading && nodeEntries == null)
            {
                ResetDiscardingSaved(recorder);
                return;
            }
            recorder.Record(ref blackboard, nameof(blackboard));
            recorder.Record(ref active, nameof(active));

            recorder.Record(ref debugCurrentFrame, nameof(debugCurrentFrame));
        }

        // Starts the tree over in place of whatever the save holds, marking the saved fields read so Dec doesn't report them as unknown
        private void ResetDiscardingSaved(Dec.Recorder recorder)
        {
            IgnoreSavedNodes(recorder);
            recorder.Ignore(nameof(blackboard));

            ResetToStart();
            loadReset = LoadReset.Full;
        }

        // Marks the saved per-node state read, in every shape a save has held it
        private static void IgnoreSavedNodes(Dec.Recorder recorder)
        {
            recorder.Ignore(nameof(nodeEntries));
            recorder.Ignore(nameof(active));
            recorder.Ignore(nameof(debugCurrentFrame));

            // the per-node arrays of saves that predate nodeEntries
            recorder.Ignore("enumerators");
            recorder.Ignore("debugLastStates");
            recorder.Ignore("debugLastStateFrames");
        }
    }
}