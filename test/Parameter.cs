
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
        public void Basic([Values] CloneBehavior cloneBehavior)
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

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);

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
        public void RegistrationFailure([Values] CloneBehavior cloneBehavior)
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

            DoCloneBehavior(cloneBehavior, ref state);

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
        public void RegistrationList([Values] CloneBehavior cloneBehavior)
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

            DoCloneBehavior(cloneBehavior, ref state);

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
        public void RegistrationArray([Values] CloneBehavior cloneBehavior)
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

            DoCloneBehavior(cloneBehavior, ref state);

            state.Blackboard().Set<string>(RegistrationArrayTree.read, "hello");
        }

        public class FromConstantTree : Arbor.TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> read = BlackboardParameter<string>.Constant("src");
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
        public void FromConstant([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new System.Type[] { typeof(FromConstantTree) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Parameter.FromConstantTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            ExpectErrors(() => state.Blackboard().Set<string>(FromConstantTree.read, "hello"));
            state.Blackboard().Set<string>(FromConstantTree.write, "goodbye");

            Assert.AreEqual("goodbye", state.Blackboard().Get<string>(FromConstantTree.write));

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);

            Assert.AreEqual("src", state.Blackboard().Get<string>(FromConstantTree.write));
        }

        public class ToConstantTree : Arbor.TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> read = BlackboardParameter<string>.Tree("read");
            public static BlackboardParameter<string> write = BlackboardParameter<string>.Constant("dst");

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
        public void ToConstant([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new System.Type[] { typeof(ToConstantTree) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Parameter.ToConstantTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            state.Blackboard().Set<string>(ToConstantTree.read, "hello");
            ExpectErrors(() => state.Blackboard().Set<string>(ToConstantTree.write, "goodbye"));

            Assert.AreEqual("dst", state.Blackboard().Get<string>(ToConstantTree.write));

            DoCloneBehavior(cloneBehavior, ref state);

            ExpectErrors(() => state.Update());

            DoCloneBehavior(cloneBehavior, ref state);

            Assert.AreEqual("dst", state.Blackboard().Get<string>(ToConstantTree.write));
        }
    }
}
