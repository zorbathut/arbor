
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
            public Scope(Tree tree, Blackboard global)
            {
                old = Current.Value;
                current = tree;
                Current.Value = current;
                current.blackboards["global"] = global;
            }

            public void Dispose()
            {
                Assert.AreSame(Current.Value, current);
                current.blackboards["global"] = null;
                Current.Value = old;
            }
        }

        private Node root;
        private Dictionary<string, Blackboard> blackboards = new Dictionary<string, Blackboard>();

        // refreshed on every update; used for event triggers
        // we don't initialize it here because that causes problems with dec serialization
        // (it shouldn't, in theory, but we have no way to specify "members are shared but the object isn't")
        internal List<Node> active;

        // ephemeral
        internal List<Node> stack = new List<Node>();

        private Tree() { }  // exists just for Dec
        public Tree(Node root, Blackboard global)
        {
            this.root = root;

            blackboards["tree"] = new Blackboard();
            active = new List<Node>();

            using (new Scope(this, global))
            {
                root?.Init();
            }
        }

        public void Update(Blackboard global)
        {
            active.Clear();
            using (new Scope(this, global))
            {
                root?.Update();
            }
        }

        public void EventInvoke(Blackboard global, EventDec ev)
        {
            EventInvokeWorker(global, ev, null);
        }

        public void EventInvoke<T1>(Blackboard global, EventDec<T1> ev, T1 param1)
        {
            EventInvokeWorker(global, ev, new object[] { param1 });
        }

        public void EventInvoke<T1, T2>(Blackboard global, EventDec<T1, T2> ev, T1 param1, T2 param2)
        {
            EventInvokeWorker(global, ev, new object[] { param1, param2 });
        }

        public void EventInvoke<T1, T2, T3>(Blackboard global, EventDec<T1, T2, T3> ev, T1 param1, T2 param2, T3 param3)
        {
            EventInvokeWorker(global, ev, new object[] { param1, param2, param3 });
        }

        public void EventInvoke<T1, T2, T3, T4>(Blackboard global, EventDec<T1, T2, T3, T4> ev, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            EventInvokeWorker(global, ev, new object[] { param1, param2, param3, param4 });
        }

        private void EventInvokeWorker(Blackboard global, BaseEventDec ev, object[] param)
        {
            using (new Scope(this, global))
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

        public Blackboard Blackboard(string id)
        {
            return blackboards[id];
        }

        internal T BlackboardGet<T>(BlackboardIdentifier identifier)
        {
            return blackboards[identifier.bb].Get<T>(identifier.id);
        }

        internal void BlackboardSet<T>(BlackboardIdentifier identifier, T item)
        {
            blackboards[identifier.bb].Set<T>(identifier.id, item);
        }

        internal void Register<T>(BlackboardIdentifier identifier)
        {
            blackboards[identifier.bb].Register(identifier.id, typeof(T));
        }

        public void Record(Dec.Recorder recorder)
        {
            recorder.Shared().Record(ref root, nameof(root));
            recorder.Shared().Record(ref active, nameof(active));
            recorder.Record(ref blackboards, nameof(blackboards));
        }
    }
}
