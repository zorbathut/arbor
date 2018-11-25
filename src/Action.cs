using System;
using System.Collections.Generic;

namespace Arbor
{
    public class Action<T> : Node<T>
    {
        private readonly Func<Context<T>, IEnumerable<Result>> action;
        private IEnumerator<Result> active;

        public Action(Func<Context<T>, IEnumerable<Result>> action, string name = null) : base(name : name)
        {
            this.action = action;
        }

        public override Result UpdateWorker(Context<T> context)
        {
            if (active == null || active.Current != Result.Working)
            {
                active = action(context).GetEnumerator();
            }

            if (active.MoveNext())
            {
                return active.Current;
            }

            // moved off the end
            active = null;
            return Result.Success;
        }

        public override void Reset()
        {
            base.Reset();

            active = null;
        }
    }
}
