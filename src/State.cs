
using System.Collections.Generic;

namespace Arbor
{
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

        // local state
        internal IEnumerator<Result>[] enumerators;
        internal Blackboard blackboard;

        // refreshed on every update; used for event triggers
        internal List<int> active;

        // ephemeral, used for debugging
        internal List<Node> stack = new List<Node>();

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
            enumerators = new IEnumerator<Result>[tree.nodes.Length];
            active = new List<int>();

            // get a copy of the initial blackboard
            blackboard = Dec.Recorder.Clone(tree.blackboardTemplate);
        }

        public void Update()
        {
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
                tree.root.Reset();
            }
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

        public void Record(Dec.Recorder recorder)
        {
            // this ends up being complicated
            if (recorder.Intent == Dec.Recorder.Purpose.Cloning)
            {
                // just copy it all over
                recorder.Record(ref tree, nameof(tree));
                recorder.Record(ref enumerators, nameof(enumerators));
                recorder.Record(ref blackboard, nameof(blackboard));
                recorder.Record(ref active, nameof(active));
                return;
            }

            recorder.Record(ref tree, nameof(tree));

            if (recorder.Mode == Dec.Recorder.Direction.Write)
            {
                recorder.Record(ref tree.blackboardSignature, "signature");
            }
            else if (recorder.Mode == Dec. Recorder.Direction.Read)
            {
                ulong sig = 0;
                recorder.Record(ref sig, "signature");

                if (sig != tree.blackboardSignature)
                {
                    Dbg.Wrn("Blackboard signature mismatch; this is likely due to a change in the tree structure or blackboard parameters. Resetting.");

                    ResetToStart();
                    return;
                }
            }

            // this is needed for identifier serialization
            using var scope = new Scope(this);

            recorder.Record(ref enumerators, nameof(enumerators));
            recorder.Record(ref blackboard, nameof(blackboard));
            recorder.Record(ref active, nameof(active));
        }
    }
}