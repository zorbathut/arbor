using System;
using System.Collections.Generic;

namespace Arbor
{
    public partial class FunctionSimple : Node
    {
        private Func<bool> condition;

        private FunctionSimple() { }  // exists just for Dec
        public FunctionSimple(Func<bool> condition)
        {
            this.condition = condition;
        }

        [Dec.RecorderEnumerator.RecordableEnumerable]
        public override IEnumerable<Result> Worker()
        {
            yield return condition() ? Result.Success : Result.Failure;
        }
    }
}
