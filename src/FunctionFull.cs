using System;
using System.Collections.Generic;

namespace Arbor
{
    public partial class FunctionFull : Node
    {
        private Func<Result> func;

        private FunctionFull() { }  // exists just for Dec
        public FunctionFull(Func<Result> func)
        {
            this.func = func;
        }

        [Dec.RecorderEnumerator.RecordableEnumerable]
        public override IEnumerable<Result> Worker()
        {
            while (true)
            {
                yield return func();
            }
        }

        public override void Record(Dec.Recorder recorder)
        {
            base.Record(recorder);

            recorder.Record(ref func, nameof(func));
        }
    }
}
