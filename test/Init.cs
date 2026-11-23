
using NUnit.Framework;
using System.Collections.Generic;
using Arbor;
using Assert = NUnit.Framework.Assert;

namespace ArborTest
{
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public partial class Init : Base
    {
        public partial class ReadDuringInitNode : Node
        {
            public BlackboardParameter<int> ReadId;

            public int DoRead()
            {
                return Read;
            }

            public override IEnumerable<Result> Worker()
            {
                yield return Result.Success;
            }
        }

        public partial class WriteOnlyTestTree : TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> testParameter = BlackboardParameter<string>.Tree("test");
            public static BlackboardParameter<int> readParameter = BlackboardParameter<int>.Tree("horse");

            public Node Create(TreeDec treeDec)
            {
                treeDec.BlackboardRegister(testParameter);

                var node = new ReadDuringInitNode() { ReadId = readParameter };

                // this should fail
                var value = node.DoRead();

                return node;
            }
        }

        [Test]
        public void WriteOnlyDuringInit()
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(WriteOnlyTestTree) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Init.WriteOnlyTestTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            ExpectErrors(() => parser.Finish(), err => err.Contains("Attempting to read from Blackboard during initialization"));

            // Verify normal reads work after initialization
            var tree = Dec.Database<TreeDec>.Get("Test");
            var state = new State(tree);
            state.Blackboard().Set(WriteOnlyTestTree.testParameter, "value");
            Assert.AreEqual("value", state.Blackboard().Get<string>(WriteOnlyTestTree.testParameter));
        }

        public partial class InitSelfNode : Node, Dec.IRecordable
        {
            public int value;
            public int doubled;
            public int initSelfCalls;
            public Node child;

            private InitSelfNode() { }  // exists just for Dec
            public InitSelfNode(int value)
            {
                this.value = value;
            }

            protected override void InitSelf()
            {
                doubled = value * 2;
                initSelfCalls++;

                if (child == null)
                {
                    child = new Succeed();
                }
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }

            public override IEnumerable<Result> Worker()
            {
                yield return Result.Success;
            }
        }

        public class InitSelfTree : TreeDec.ITreeFactory
        {
            public static Node pending;

            public Node Create(TreeDec treeDec)
            {
                return new Arbor.Sequence(pending);
            }
        }

        // A recorder read fills a node's fields after its parameterless constructor has run, so InitSelf is the one place that sees them however the node was built.
        [Test]
        public void InitSelfSeesFinalFields([Values] bool recorded)
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(InitSelfTree), typeof(InitSelfNode) }
            });

            var node = new InitSelfNode(21);
            if (recorded)
            {
                node = Dec.Recorder.Read<InitSelfNode>(Dec.Recorder.Write(node));
            }
            Assert.AreEqual(0, node.initSelfCalls);

            InitSelfTree.pending = node;
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Init.InitSelfTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();
            InitSelfTree.pending = null;

            Assert.AreEqual(42, node.doubled);
            Assert.AreEqual(1, node.initSelfCalls);

            // the child InitSelf filled in joined the tree after it
            var tree = Dec.Database<TreeDec>.Get("Test");
            Assert.AreEqual(3, tree.nodes.Length);
            Assert.AreSame(node.child, tree.nodes[2]);
        }
    }
}