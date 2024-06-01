namespace ArborTest
{
    using NUnit.Framework;
    using System.Collections.Generic;

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
    }
}