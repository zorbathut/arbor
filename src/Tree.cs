
using System.Collections.Generic;

namespace Arbor
{
    public class Tree : Dec.IRecordable
    {
        public static System.Threading.ThreadLocal<Tree> Current = new();
        private struct Scope : System.IDisposable
        {
            private Tree old;
            private Tree current;
            public Scope(Tree tree)
            {
                old = Current.Value;
                current = tree;
                Current.Value = current;
            }

            public void Dispose()
            {
                Assert.AreSame(Current.Value, current);
                Current.Value = old;
            }
        }

        private Node root;
        private Blackboard blackboard;

        // refreshed on every update; used for event triggers
        // we don't initialize it here because that causes problems with dec serialization
        // (it shouldn't, in theory, but we have no way to specify "members are shared but the object isn't")
        internal List<Node> active;

        // ephemeral
        internal List<Node> stack = new List<Node>();

        private Tree() { }  // exists just for Dec
        public Tree(Node root)
        {
            this.root = root;

            active = new List<Node>();

            blackboard = new Blackboard();

            using (new Scope(this))
            {
                root?.Init();
            }
        }

        public void Update()
        {
            active.Clear();
            using (new Scope(this))
            {
                root?.Update();
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
                foreach (var node in active)
                {
                    if (node?.eventActions?.TryGetValue(ev, out var actions) ?? false)
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
                if (active[i]?.properties?.TryGetValue(prop, out var value) ?? false)
                {
                    return (T)value;
                }
            }

            return default;
        }

        public void Reset()
        {
            root.Reset();
        }

        public Blackboard Blackboard()
        {
            return blackboard;
        }

        internal T BlackboardGet<T>(BlackboardIdentifier identifier)
        {
            return blackboard.Get<T>(identifier.id);
        }

        internal void BlackboardSet<T>(BlackboardIdentifier identifier, T item)
        {
            blackboard.Set<T>(identifier.id, item);
        }

        internal void Register<T>(BlackboardIdentifier identifier)
        {
            blackboard.Register(identifier.id, typeof(T));
        }

        public void Record(Dec.Recorder recorder)
        {
            recorder.Shared().Record(ref root, nameof(root));
            recorder.Shared().Record(ref active, nameof(active));
            recorder.Record(ref blackboard, nameof(blackboard));
        }
    }
}
