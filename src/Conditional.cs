using System;
using System.Collections.Generic;

namespace Arbor
{
    public class Conditional<T> : Node<T>
    {
        private readonly Func<Context<T>, bool> condition;

        public Conditional(Func<Context<T>, bool> condition, string name = null) : base(name : name)
        {
            this.condition = condition;
        }

        public override Result UpdateWorker(Context<T> context)
        {
            return condition(context) ? Result.Success : Result.Failure;
        }
    }
}
