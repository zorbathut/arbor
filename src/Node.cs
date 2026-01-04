using System.Collections.Generic;
using Dec;

namespace Arbor
{
    public enum Result
    {
        Success,
        Working,
        Failure,
    }

    public abstract partial class Node
    {
        internal Dictionary<Arbor.BaseEventDec, List<System.Delegate>> eventActions;
        internal Dictionary<Arbor.BasePropertyDec, object> properties;

        private bool initted;
        internal int nodeIndex = -1;

        internal static bool initRunning = false;

        public void Init(TreeDec treeDec, List<Node> nodeList)
        {
            if (!initRunning)
            {
                Dbg.Err("Init must be called from within a full tree init; individual nodes cannot be initted independently!");
                return;
            }

            if (initted)
            {
                Dbg.Err("Initted multiple times");
            }

            // add to the node list
            Assert.AreEqual(-1, nodeIndex);
            nodeIndex = nodeList.Count;
            nodeList.Add(this);

            InitFields(treeDec, nodeList);

            initted = true;
        }

        public Result Update()
        {
            if (!initted)
            {
                Dbg.Err("Not initted");
            }

            var state = State.Current.Value;
            state.stack.Add(this);

            // get it in the tree in the right order
            int activeIndex = state.active.Count;
            state.active.Add(nodeIndex);

            ref var currentWorker = ref state.enumerators[nodeIndex];
            bool moved;
            try
            {
                if (currentWorker == null)
                {
                    currentWorker = Worker().GetEnumerator();
                }

                moved = currentWorker.MoveNext();
            }
            catch (System.Exception e)
            {
                Dbg.Ex(e);
                moved = false;
            }
            state.stack.RemoveAt(state.stack.Count - 1);

            if (!moved)
            {
                Dbg.Err("Worker didn't exit properly, assuming failure");
                try
                {
                    Reset();
                }
                catch (System.Exception e)
                {
                    Dbg.Ex(e);
                }

                state.active[activeIndex] = -1; // nope, not active anymore
                state.debugLastStates[nodeIndex] = DebugNodeState.Failure;
                state.debugLastStateFrames[nodeIndex] = state.debugCurrentFrame;
                return Result.Failure;
            }

            var result = currentWorker.Current;
            if (result != Result.Working)
            {
                // we done now
                try
                {
                    Reset();
                }
                catch (System.Exception e)
                {
                    Dbg.Ex(e);
                }
                state.active[activeIndex] = -1; // nope, not active anymore
                state.debugLastStates[nodeIndex] = result == Result.Success
                    ? DebugNodeState.Success
                    : DebugNodeState.Failure;
                state.debugLastStateFrames[nodeIndex] = state.debugCurrentFrame;
            }

            return result;
        }

        internal void EventAttach_Internal(Arbor.BaseEventDec eve, System.Delegate deleg)
        {
            if (eventActions == null)
            {
                eventActions = new Dictionary<Arbor.BaseEventDec, List<System.Delegate>>();
            }

            if (!eventActions.TryGetValue(eve, out var actions))
            {
                actions = new List<System.Delegate>();
                eventActions[eve] = actions;
            }

            actions.Add(deleg);
        }

        internal void PropertyAttach_Internal(Arbor.BasePropertyDec eve, object data)
        {
            if (properties == null)
            {
                properties = new Dictionary<Arbor.BasePropertyDec, object>();
            }

            properties[eve] = data;
        }

        public abstract IEnumerable<Result> Worker();

        public virtual void Reset()
        {
            var state = State.Current.Value;
            ref var currentWorker = ref state.enumerators[nodeIndex];

            // already done, stop recursiving
            if (currentWorker == null)
            {
                return;
            }

            // Mark as terminated since we're being reset mid-execution
            state.debugLastStates[nodeIndex] = DebugNodeState.Terminated;
            state.debugLastStateFrames[nodeIndex] = state.debugCurrentFrame;

            currentWorker.Dispose();
            currentWorker = null;
            ResetFields();
        }

        public virtual void InitFields(TreeDec treeDec, List<Node> nodeList) { }
        public virtual void ResetFields() { }
    }
}
