namespace ArborTest
{
    using Arbor;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public class Sequence : Base
    {
        static int stage1_seen = 0;
        static Result stage2_rf = Result.Working;
        static int stage3_seen = 0;
        static Result stage4_rf = Result.Working;
        static int stage5_seen = 0;
        static Result stage6_rf = Result.Working;
        static int stage7_seen = 0;

        [Test]
        public void Basic([Values] CloneBehavior cloneBehavior)
        {
            stage1_seen = 0;
            stage2_rf = Result.Working;
            stage3_seen = 0;
            stage4_rf = Result.Working;
            stage5_seen = 0;
            stage6_rf = Result.Working;
            stage7_seen = 0;

            var blackboardGlobal = new Blackboard();
            Arbor.Tree tree = new Arbor.Tree(new Arbor.Sequence(new List<Arbor.Node> {
                new Function(() =>
                {
                    stage1_seen++;
                    return true;
                }),
                new ResultFunction(() => stage2_rf),
                new Function(() =>
                {
                    stage3_seen++;
                    return true;
                }),
                new ResultFunction(() => stage4_rf),
                new Function(() =>
                {
                    stage5_seen++;
                    return true;
                }),
                new ResultFunction(() => stage6_rf),
                new Function(() =>
                {
                    stage7_seen++;
                    return true;
                }),
            }), blackboardGlobal);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);
            tree.Update(blackboardGlobal);

            Assert.AreEqual(1, stage1_seen);
            Assert.AreEqual(0, stage3_seen);
            Assert.AreEqual(0, stage5_seen);
            Assert.AreEqual(0, stage7_seen);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);
            tree.Update(blackboardGlobal);

            Assert.AreEqual(1, stage1_seen);
            Assert.AreEqual(0, stage3_seen);
            Assert.AreEqual(0, stage5_seen);
            Assert.AreEqual(0, stage7_seen);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);
            stage2_rf = Result.Success;
            tree.Update(blackboardGlobal);

            Assert.AreEqual(1, stage1_seen);
            Assert.AreEqual(1, stage3_seen);
            Assert.AreEqual(0, stage5_seen);
            Assert.AreEqual(0, stage7_seen);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);
            stage4_rf = Result.Failure;
            tree.Update(blackboardGlobal);

            Assert.AreEqual(1, stage1_seen);
            Assert.AreEqual(1, stage3_seen);
            Assert.AreEqual(0, stage5_seen);
            Assert.AreEqual(0, stage7_seen);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);
            tree.Update(blackboardGlobal);

            Assert.AreEqual(2, stage1_seen);
            Assert.AreEqual(2, stage3_seen);
            Assert.AreEqual(0, stage5_seen);
            Assert.AreEqual(0, stage7_seen);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);
            stage4_rf = Result.Success;
            stage6_rf = Result.Success;
            tree.Update(blackboardGlobal);

            Assert.AreEqual(3, stage1_seen);
            Assert.AreEqual(3, stage3_seen);
            Assert.AreEqual(1, stage5_seen);
            Assert.AreEqual(1, stage7_seen);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);
        }
    }
}