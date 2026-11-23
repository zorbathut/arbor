using NUnit.Framework;
using System.Collections.Generic;

namespace ArborTest
{
    [TestFixture]
    public partial class Generator : Base
    {

        // this looks like it's not used, but what we're *really* checking here is whether the generator can tolerate two classes with the same name
        public partial class One
        {
            public partial class SameNameTestNode : Arbor.Node
            {
                public Arbor.BlackboardParameter<int> ReadId;

                public override IEnumerable<Arbor.Result> Worker()
                {
                    yield return Arbor.Result.Success;
                }
            }
        }
        public partial class Two
        {
            public partial class SameNameTestNode : Arbor.Node
            {
                public Arbor.BlackboardParameter<int> ReadId;

                public override IEnumerable<Arbor.Result> Worker()
                {
                    yield return Arbor.Result.Success;
                }
            }
        }

        // One node type in two declarations, which the generator must emit once; a second emission of the same hint name makes Roslyn drop all of its output, and nothing here compiles.
        public partial class SplitTestNode : Arbor.Node
        {
            public Arbor.BlackboardParameter<int> ReadId;
        }

        public partial class SplitTestNode
        {
            public override IEnumerable<Arbor.Result> Worker()
            {
                yield return Arbor.Result.Success;
            }
        }
    }
}