
using NUnit.Framework;
using System.Collections.Generic;
using Arbor;

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
            UpdateTestParameters(new Dec.Config.UnitTestParameters {
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
    }
}