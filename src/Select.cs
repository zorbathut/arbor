
using System.Collections.Generic;

namespace Arbor
{
    public partial class Select : Node
    {
        private Select() { }  // exists just for Dec
        public Select(params Node[] children)
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
                    var result = child.Update();
                    if (result == Result.Failure)
                    {
                        // whoops! abort this, hide the failure, try another one
                        break;
                    }

                    // either success, in which case we're done, or working
                    yield return result;
                }
            }

            // I guess we iterated through everything!
            yield return Result.Failure;
        }

        public override void Record(Dec.Recorder recorder)
        {
            base.Record(recorder);

            recorder.Shared().Record(ref m_children, nameof(m_children));
        }
    }
}
