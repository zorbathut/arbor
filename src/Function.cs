using System;
using System.Collections.Generic;

namespace Arbor
{
    public partial class Function : Node
    {
        private readonly Func<bool> condition;

        public Function(Func<bool> condition)
        {
            this.condition = condition;
        }

        public override IEnumerable<Result> Worker()
        {
            yield return condition() ? Result.Success : Result.Failure;
        }
    }
}
