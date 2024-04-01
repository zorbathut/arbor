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
        private IEnumerator<Result> currentWorker;

        internal void Init()
        {
            InitFields();
        }

        public Result Update()
        {
            var tree = Tree.Current.Value;
            tree.stack.Add(this);

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
            tree.stack.RemoveAt(tree.stack.Count - 1);

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
            }

            return result;
        }

        public abstract IEnumerable<Result> Worker();

        public virtual void Reset()
        {
            // already done, stop recursiving
            if (currentWorker == null)
            {
                return;
            }

            currentWorker = null;
            ResetFields();
        }

        public virtual void InitFields() { }
        public virtual void ResetFields() { }

        public virtual void Record(Recorder recorder)
        {
            recorder.Record(ref currentWorker, nameof(currentWorker));
        }
    }
}
