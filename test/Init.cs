
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
            public BlackboardParameter<string> ReadId = BlackboardParameter<string>.Tree("test");
            private bool readDuringInit = false;

            public override IEnumerable<Result> Worker()
            {
                if (!readDuringInit)
                {
                    // Attempt to read during init
                    var nothing = Read;
                }
                yield return Result.Success;
            }
        }

        public class WriteOnlyTestTree : TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> testParameter = BlackboardParameter<string>.Tree("test");
            public static BlackboardParameter<int> horseParameter = BlackboardParameter<int>.Tree("horse");

            public Node Create(Blackboard blackboardDescriptor)
            {
                blackboardDescriptor.Register(testParameter);

                int x = blackboardDescriptor.Get<int>(horseParameter);

                return new Succeed();
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