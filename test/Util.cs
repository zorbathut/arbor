using Arbor;
using System;
using System.Collections.Generic;

namespace ArborTest
{
    [Dec.RecorderEnumerator.RecordableClosures]
    public partial class ResultFunction : Node
    {
        private Func<Result> condition;

        private ResultFunction() { }  // exists just for Dec
        public ResultFunction(Func<Result> condition)
        {
            this.condition = condition;
        }

        [Dec.RecorderEnumerator.RecordableEnumerable]
        public override IEnumerable<Result> Worker()
        {
            while (true)
            {
                yield return condition();
            }
        }

        public override void Record(Dec.Recorder recorder)
        {
            base.Record(recorder);

            recorder.Record(ref condition, nameof(condition));
        }
    }

    public partial class WaitNode : Node
    {
        [Dec.RecorderEnumerator.RecordableEnumerable]
        public override IEnumerable<Result> Worker()
        {
            while (true)
            {
                yield return Result.Working;
            }
        }
    }
}
