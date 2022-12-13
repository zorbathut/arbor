
using System.Collections.Generic;

namespace Arbor
{
    public partial class Select : Node
    {
        public Select(List<Node> children) : base(children) { }
        
        public override IEnumerable<Result> Worker()
        {
            foreach (var child in Children)
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
    }
}
