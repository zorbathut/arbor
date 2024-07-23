
using System.Collections.Generic;

namespace Arbor
{
    public class TreeInstance : Dec.IRecordable
    {
        public static System.Threading.ThreadLocal<TreeInstance> Current = new();
        private struct Scope : System.IDisposable
        {
            private TreeInstance old;
            private TreeInstance current;
            public Scope(TreeInstance treeInstance, Blackboard global)
            {
                old = Current.Value;
                current = treeInstance;
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

        public TreeDec treeDec;
        internal List<IEnumerator<Result>> workers;

        private Dictionary<string, Blackboard> blackboards = new Dictionary<string, Blackboard>();

        // refreshed on every update; used for event triggers
        // we don't initialize it here because that causes problems with dec serialization
        // (it shouldn't, in theory, but we have no way to specify "members are shared but the object isn't")
        internal List<Node> active;

        private TreeInstance() { }  // exists just for Dec
        public TreeInstance(TreeDec treeDec, Blackboard global)
        {
            this.treeDec = treeDec;

            // preallocate worker space
            workers = new List<IEnumerator<Result>>(treeDec.nodes.Count);
            for (int i = 0; i < treeDec.nodes.Count; i++)
            {
                workers.Add(null);
            }

            blackboards["tree"] = new Blackboard();
            active = new List<Node>();

            // go through the tree items and mark up the blackboards
            using (new Scope(this, global))
            {
                for (int i = 0; i < treeDec.nodes.Count; ++i)
                {
                    treeDec.nodes[i].Init();
                }
            }
        }

        public void Update(Blackboard global)
        {
            active.Clear();

            if (treeDec.nodes.Count > 0)
            {
                using (new Scope(this, global))
                {
                    TreeExecution.Update(treeDec.nodes[0]);
                }
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

        public void Reset()
        {
            using var scope = new Scope(this, null);

            // always start at zero
            TreeExecution.Terminate(0);
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
            recorder.Record(ref treeDec, nameof(treeDec));
            recorder.Shared().Record(ref workers, nameof(workers));
            recorder.Shared().Record(ref active, nameof(active));
            recorder.Record(ref blackboards, nameof(blackboards));
        }
    }
}
