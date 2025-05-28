
using Arbor;
using NUnit.Framework;
using System.Collections.Generic;

namespace ArborTest
{
    [TestFixture]
    public partial class Serialization : Base
    {
        public partial class DoNothingWith : Arbor.Node
        {
            public BlackboardParameter<string> DataId;

            public override IEnumerable<Result> Worker()
            {
                // Just a placeholder to do nothing
                yield return Result.Success;
            }
        }

        public class ParameterTreeA : Arbor.TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> data = BlackboardParameter<string>.Tree("data");

            public Node Create(TreeDec tree)
            {
                return new DoNothingWith() { DataId = data };
            }
        }

        public class ParameterTreeB : Arbor.TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> data = BlackboardParameter<string>.Tree("data");

            public Node Create(TreeDec tree)
            {
                return new DoNothingWith() { DataId = data };
            }
        }

        [Test]
        public void ChangedUid()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new System.Type[] { typeof(ParameterTreeA), typeof(ParameterTreeB) } });

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <Arbor.TreeDec decName=""Test"">
                            <worker class=""ArborTest.Serialization.ParameterTreeA"" />
                        </Arbor.TreeDec>
                    </Decs>
                ");
                parser.Finish();
            }

            string serialized;
            {
                var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));
                state.Blackboard().Set(ParameterTreeA.data, "banjo");

                serialized = Dec.Recorder.Write(state);
            }

            Dec.Database.Clear();

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <Arbor.TreeDec decName=""Test"">
                            <worker class=""ArborTest.Serialization.ParameterTreeB"" />
                        </Arbor.TreeDec>
                    </Decs>
                ");
                parser.Finish();
            }

            {
                var newState = Dec.Recorder.Read<Arbor.State>(serialized);
                Assert.AreEqual("banjo", newState.Blackboard().Get(ParameterTreeB.data));
                newState.Blackboard().Set(ParameterTreeB.data, "newBanjo");
            }

            Assert.AreNotEqual(ParameterTreeA.data, ParameterTreeB.data);
        }
    }
}