namespace ArborTest
{
    using Arbor;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public partial class Sequence : Base
    {
        [Test]
        public void Basic()
        {
            int stage1_seen = 0;
            Result stage2_rf = Result.Working;
            int stage3_seen = 0;
            Result stage4_rf = Result.Working;
            int stage5_seen = 0;
            Result stage6_rf = Result.Working;
            int stage7_seen = 0;

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

            tree.Update(blackboardGlobal);

            Assert.AreEqual(1, stage1_seen);
            Assert.AreEqual(0, stage3_seen);
            Assert.AreEqual(0, stage5_seen);
            Assert.AreEqual(0, stage7_seen);

            tree.Update(blackboardGlobal);

            Assert.AreEqual(1, stage1_seen);
            Assert.AreEqual(0, stage3_seen);
            Assert.AreEqual(0, stage5_seen);
            Assert.AreEqual(0, stage7_seen);

            stage2_rf = Result.Success;
            tree.Update(blackboardGlobal);

            Assert.AreEqual(1, stage1_seen);
            Assert.AreEqual(1, stage3_seen);
            Assert.AreEqual(0, stage5_seen);
            Assert.AreEqual(0, stage7_seen);

            stage4_rf = Result.Failure;
            tree.Update(blackboardGlobal);

            Assert.AreEqual(1, stage1_seen);
            Assert.AreEqual(1, stage3_seen);
            Assert.AreEqual(0, stage5_seen);
            Assert.AreEqual(0, stage7_seen);

            tree.Update(blackboardGlobal);

            Assert.AreEqual(2, stage1_seen);
            Assert.AreEqual(2, stage3_seen);
            Assert.AreEqual(0, stage5_seen);
            Assert.AreEqual(0, stage7_seen);

            stage4_rf = Result.Success;
            stage6_rf = Result.Success;
            tree.Update(blackboardGlobal);

            Assert.AreEqual(3, stage1_seen);
            Assert.AreEqual(3, stage3_seen);
            Assert.AreEqual(1, stage5_seen);
            Assert.AreEqual(1, stage7_seen);
        }
    }
}