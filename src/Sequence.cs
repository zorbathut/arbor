
using System.Collections.Generic;

namespace Arbor
{
    public partial class Sequence : Node
    {
        private Sequence() { }  // exists just for Dec
        public Sequence(params Node[] children)
        {
            m_children = children;
        }

        private Node[] m_children;

        [Dec.RecorderEnumerator.RecordableEnumerable]
        public override IEnumerable<Result> Worker()
        {
            foreach (var child in m_children)
            {
                while (true)
                {
                    var result = TreeExecution.Update(child);
                    if (result == Result.Success)
                    {
                        // yay! keep on movin'
                        break;
                    }

                    // either failure, in which case we're done, or working
                    yield return result;
                }
            }

            // I guess we iterated through everything!
            yield return Result.Success;
        }

        public override void Record(Dec.Recorder recorder)
        {
            base.Record(recorder);

            recorder.Shared().Record(ref m_children, nameof(m_children));
        }
    }
}
