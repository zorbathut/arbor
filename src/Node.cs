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

    public abstract partial class Node : Dec.IRecordable
    {
        public List<int> childLinks = new List<int>();
        internal Dictionary<Arbor.BaseEventDec, List<System.Delegate>> eventActions;

        public virtual void Init() { }

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

        public abstract IEnumerable<Result> Worker();

        public virtual int UnrollTo(TreeDec treeDec)
        {
            return treeDec.RegisterUnrollNode(this);
        }

        public virtual void Record(Recorder recorder)
        {
            recorder.Record(ref childLinks, nameof(childLinks));
            recorder.Record(ref eventActions, nameof(eventActions));
        }
    }
}
