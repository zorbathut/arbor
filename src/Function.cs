using System;
using System.Collections.Generic;

namespace Arbor
{
    public partial class Function : Node
    {
        private Func<bool> condition;

        private Function() { }  // exists just for Dec
        public Function(Func<bool> condition)
        {
            this.condition = condition;
        }

        [Dec.RecorderEnumerator.RecordableEnumerable]
        public override IEnumerable<Result> Worker()
        {
            yield return condition() ? Result.Success : Result.Failure;
        }

        public override void Record(Dec.Recorder recorder)
        {
            base.Record(recorder);

            recorder.Record(ref condition, nameof(condition));
        }
    }
}
