
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

        public class BasicTree : Arbor.TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> read = BlackboardParameter<string>.Tree("read");
            public static BlackboardParameter<string> write = BlackboardParameter<string>.Tree("write");

            public Node Create(TreeDec tree)
            {
                return new ParameterTestNode()
                {
                    ReadId = read,
                    WriteId = write,
                };
            }
        }

        [Test]
        public void Basic()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new System.Type[] { typeof(BasicTree) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Parameter.BasicTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            state.Blackboard().Set<string>(BasicTree.read, "hello");
            state.Blackboard().Set<string>(BasicTree.write, "goodbye");

            Assert.AreEqual("goodbye", state.Blackboard().Get<string>(BasicTree.write));

            state.Update();

            Assert.AreEqual("hello", state.Blackboard().Get<string>(BasicTree.write));
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

        public class RegistrationFailureTree : Arbor.TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree)
            {
                return new ListChild();
            }
        }

        [Test]
        public void RegistrationFailure()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new System.Type[] { typeof(RegistrationFailureTree) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Parameter.RegistrationFailureTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            ExpectErrors(() => state.Blackboard().Set<string>(BlackboardParameter<string>.Tree("write"), "hello"));
        }

        public class RegistrationListTree : Arbor.TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> read = BlackboardParameter<string>.Tree("read");
            public static BlackboardParameter<string> write = BlackboardParameter<string>.Tree("write");

            public Node Create(TreeDec tree)
            {
                return new ListChild(
                    new ParameterTestNode() {
                        ReadId = read,
                        WriteId = write,
                    }
                );
            }
        }

        [Test]
        public void RegistrationList()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new System.Type[] { typeof(RegistrationListTree) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Parameter.RegistrationListTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            state.Blackboard().Set<string>(RegistrationListTree.read, "hello");
        }

        public class RegistrationArrayTree : Arbor.TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> read = BlackboardParameter<string>.Tree("read");
            public static BlackboardParameter<string> write = BlackboardParameter<string>.Tree("write");

            public Node Create(TreeDec tree)
            {
                return new ArrayChild(
                    new ParameterTestNode() {
                        ReadId = read,
                        WriteId = write,
                    }
                );
            }
        }

        [Test]
        public void RegistrationArray()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new System.Type[] { typeof(RegistrationArrayTree) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Parameter.RegistrationArrayTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            state.Blackboard().Set<string>(RegistrationArrayTree.read, "hello");
        }
    }
}
