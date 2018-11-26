using System;
using System.Collections.Generic;

namespace Arbor
{
    public class Function<T> : Node<T>
    {
        private readonly Func<Context<T>, T, bool> condition;

        public Function(Func<Context<T>, T, bool> condition, string name = null) : base(name : name)
        {
            this.condition = condition;
        }

        public override Result UpdateWorker(Context<T> context, T state)
        {
            return condition(context, state) ? Result.Success : Result.Failure;
        }
    }
}
