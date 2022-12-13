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
            Arbor.Tree tree = new Arbor.Tree(new ParameterTestNode() {
                ReadId = Arbor.BlackboardParameter<string>.Global("read"),
                WriteId = Arbor.BlackboardParameter<string>.Global("write"),
            });

            Arbor.Blackboard.Global.Set<string>("read", "hello");
            Arbor.Blackboard.Global.Set<string>("write", "goodbye");

            Assert.AreEqual("goodbye", Arbor.Blackboard.Global.Get<string>("write"));

            tree.Update();

            Assert.AreEqual("hello", Arbor.Blackboard.Global.Get<string>("write"));
        }
    }
}
