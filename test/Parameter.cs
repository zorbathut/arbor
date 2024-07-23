
using System.Linq;
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
            Arbor.TreeInstance treeInstance = new Arbor.TreeInstance(CreateDec(new ParameterTestNode() {
                ReadId = Arbor.BlackboardParameter<string>.Global("read"),
                WriteId = Arbor.BlackboardParameter<string>.Global("write"),
            }), blackboardGlobal);

            blackboardGlobal.Set<string>("read", "hello");
            blackboardGlobal.Set<string>("write", "goodbye");

            Assert.AreEqual("goodbye", blackboardGlobal.Get<string>("write"));

            treeInstance.Update(blackboardGlobal);

            Assert.AreEqual("hello", blackboardGlobal.Get<string>("write"));
        }

        public partial class ListChild : Arbor.Node
        {
            private List<Node> children;

            private ListChild() { }
            public ListChild(params Node[] children)
            {
                this.children = children.ToList();
            }

            public override IEnumerable<Arbor.Result> Worker()
            {
                yield return Arbor.Result.Success;
            }
        }

        public partial class ArrayChild : Arbor.Node
        {
            private Node[] children;

            private ArrayChild() { }
            public ArrayChild(params Node[] children)
            {
                this.children = children;
            }

            public override IEnumerable<Arbor.Result> Worker()
            {
                yield return Arbor.Result.Success;
            }
        }

        [Test]
        public void RegistrationFailure()
        {
            var blackboardGlobal = new Blackboard();
            Arbor.TreeInstance treeInstance = new Arbor.TreeInstance(CreateDec(new ListChild(
            )), blackboardGlobal);

            ExpectErrors(() => blackboardGlobal.Set<string>("read", "hello"));
        }

        [Test]
        public void RegistrationList()
        {
            var blackboardGlobal = new Blackboard();
            Arbor.TreeInstance treeInstance = new Arbor.TreeInstance(CreateDec(new ListChild(
                new ParameterTestNode() {
                    ReadId = Arbor.BlackboardParameter<string>.Global("read"),
                    WriteId = Arbor.BlackboardParameter<string>.Global("write"),
                }
            )), blackboardGlobal);

            blackboardGlobal.Set<string>("read", "hello");
        }

        [Test]
        public void RegistrationArray()
        {
            var blackboardGlobal = new Blackboard();
            Arbor.TreeInstance treeInstance = new Arbor.TreeInstance(CreateDec(new ArrayChild(
                new ParameterTestNode() {
                    ReadId = Arbor.BlackboardParameter<string>.Global("read"),
                    WriteId = Arbor.BlackboardParameter<string>.Global("write"),
                }
            )), blackboardGlobal);

            blackboardGlobal.Set<string>("read", "hello");
        }
    }
}
