using Arbor;
using NUnit.Framework;
using System.Collections.Generic;

namespace ArborTest
{
    [TestFixture]
    public partial class Reset : Base
    {
        [Dec.RecorderEnumerator.RecordableClosures]
        private class IncrementNode : Node
        {
            public static int IncrementValue;

            public override IEnumerable<Result> Worker()
            {
                IncrementValue++;
                yield return Result.Success;
            }
        }

        public class BasicTree : Arbor.TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree)
            {
                return new Arbor.Sequence(
                    new IncrementNode(),
                    new WaitNode(),
                    new IncrementNode()
                );
            }
        }

        [Test]
        public void Basic([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(BasicTree) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Reset.BasicTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            IncrementNode.IncrementValue = 0;
            Assert.AreEqual(0, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update();
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update();
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update();
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref state);

            state.Reset();
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update();
            Assert.AreEqual(2, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update();
            Assert.AreEqual(2, IncrementNode.IncrementValue);
        }
    }
}
