using Arbor;

namespace ArborTest
{
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public partial class Parameter : Base
    {
        public partial class ParameterTestNode : Arbor.Node
        {
            public Arbor.BlackboardParameter<string> ReadId;
            public Arbor.BlackboardParameter<string> WriteId;

            public override IEnumerable<Arbor.Result> Worker()
            {
                Write = Read;

                yield return Arbor.Result.Success;
            }
        }

        [Test]
        public void Basic()
        {
            var blackboardGlobal = new Blackboard();
            Arbor.Tree tree = new Arbor.Tree(new ParameterTestNode() {
                ReadId = Arbor.BlackboardParameter<string>.Global("read"),
                WriteId = Arbor.BlackboardParameter<string>.Global("write"),
            }, blackboardGlobal);

            blackboardGlobal.Set<string>("read", "hello");
            blackboardGlobal.Set<string>("write", "goodbye");

            Assert.AreEqual("goodbye", blackboardGlobal.Get<string>("write"));

            tree.Update(blackboardGlobal);

            Assert.AreEqual("hello", blackboardGlobal.Get<string>("write"));
        }
    }
}
