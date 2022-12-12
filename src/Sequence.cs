
using System.Collections.Generic;

namespace Arbor
{
    public class Sequence : Node
    {
        public Sequence(List<Node> children) : base(children) { }

        public override IEnumerable<Result> Worker()
        {
            foreach (var child in GetChildren())
            {
                while (true)
                {
                    var result = child.Update();
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
    }
}

