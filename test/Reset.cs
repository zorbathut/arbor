namespace ArborTest
{
    using Arbor;
    using NUnit.Framework;
    using System.Collections.Generic;

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
            var blackboardGlobal = new Blackboard();
            var tree = new Arbor.Tree(new Arbor.Sequence(
                new IncrementNode(),
                new WaitNode(),
                new IncrementNode()
            ), blackboardGlobal);

            IncrementNode.IncrementValue = 0;
            Assert.AreEqual(0, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            tree.Update(blackboardGlobal);
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            tree.Update(blackboardGlobal);
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            tree.Update(blackboardGlobal);
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            tree.Reset();
            Assert.AreEqual(1, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            tree.Update(blackboardGlobal);
            Assert.AreEqual(2, IncrementNode.IncrementValue);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            tree.Update(blackboardGlobal);
            Assert.AreEqual(2, IncrementNode.IncrementValue);
        }
    }
}