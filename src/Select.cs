
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
            for (int i = 0; i < m_children.Length; i++)
            {
                while (true)
                {
                    var result = m_children[i]?.Update() ?? Result.Failure;
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
    }
}
