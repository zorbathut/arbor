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

        [Test]
        public void Basic([Values] CloneBehavior cloneBehavior)
        {
            var tree = new Arbor.Tree(new Arbor.Sequence(
                new IncrementNode(),
                new WaitNode(),
                new IncrementNode()
            ));

            IncrementNode.IncrementValue = 0;
            Assert.AreEqual(0, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree);

            tree.Update();
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree);

            tree.Update();
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree);

            tree.Update();
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree);

            tree.Reset();
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree);

            tree.Update();
            Assert.AreEqual(2, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree);

            tree.Update();
            Assert.AreEqual(2, IncrementNode.IncrementValue);
        }
    }
}