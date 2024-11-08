
using System.Linq;
using Arbor;
using NUnit.Framework;
using System.Collections.Generic;

namespace ArborTest
{
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
                ReadId = Arbor.BlackboardParameter<string>.Tree("read"),
                WriteId = Arbor.BlackboardParameter<string>.Tree("write"),
            });

            tree.Blackboard().Set<string>("read", "hello");
            tree.Blackboard().Set<string>("write", "goodbye");

            Assert.AreEqual("goodbye", tree.Blackboard().Get<string>("write"));

            tree.Update();

            Assert.AreEqual("hello", tree.Blackboard().Get<string>("write"));
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
            Arbor.Tree tree = new Arbor.Tree(new ListChild(
            ));

            ExpectErrors(() => tree.Blackboard().Set<string>("read", "hello"));
        }

        [Test]
        public void RegistrationList()
        {
            Arbor.Tree tree = new Arbor.Tree(new ListChild(
                new ParameterTestNode() {
                    ReadId = Arbor.BlackboardParameter<string>.Tree("read"),
                    WriteId = Arbor.BlackboardParameter<string>.Tree("write"),
                }
            ));

            tree.Blackboard().Set<string>("read", "hello");
        }

        [Test]
        public void RegistrationArray()
        {
            Arbor.Tree tree = new Arbor.Tree(new ArrayChild(
                new ParameterTestNode() {
                    ReadId = Arbor.BlackboardParameter<string>.Tree("read"),
                    WriteId = Arbor.BlackboardParameter<string>.Tree("write"),
                }
            ));

            tree.Blackboard().Set<string>("read", "hello");
        }
    }
}
