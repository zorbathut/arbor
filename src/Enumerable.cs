using System;
using System.Collections.Generic;

namespace Arbor
{
    public class Enumerable<T> : Node<T>
    {
        private readonly Func<Context<T>, T, IEnumerable<Result>> action;
        private IEnumerator<Result> active;

        public Enumerable(Func<Context<T>, T, IEnumerable<Result>> action, string name = null) : base(name : name)
        {
            this.action = action;
        }

        public override Result UpdateWorker(Context<T> context, T state)
        {
            if (active == null || active.Current != Result.Working)
            {
                active = action(context, state).GetEnumerator();
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
