using System;
using Arbor;
using NUnit.Framework;

namespace ArborTest
{
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public class Sequence : Base
    {
        public class SequenceWorker : TreeDec.ITreeFactory
        {
            public static int stage1_seen = 0;
            public static Result stage2_rf = Result.Working;
            public static int stage3_seen = 0;
            public static Result stage4_rf = Result.Working;
            public static int stage5_seen = 0;
            public static Result stage6_rf = Result.Working;
            public static int stage7_seen = 0;

            public Arbor.Node Create(Blackboard blackboardDescriptor)
            {
                return new Arbor.Sequence(
                    new FunctionSimple(() =>
                    {
                        stage1_seen++;
                        return true;
                    }),
                    new ResultFunction(() => stage2_rf),
                    new FunctionSimple(() =>
                    {
                        stage3_seen++;
                        return true;
                    }),
                    new ResultFunction(() => stage4_rf),
                    new FunctionSimple(() =>
                    {
                        stage5_seen++;
                        return true;
                    }),
                    new ResultFunction(() => stage6_rf),
                    new FunctionSimple(() =>
                    {
                        stage7_seen++;
                        return true;
                    })
                );
            }
        }

        [Test]
        public void Basic([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SequenceWorker) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Sequence.SequenceWorker"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            SequenceWorker.stage1_seen = 0;
            SequenceWorker.stage2_rf = Result.Working;
            SequenceWorker.stage3_seen = 0;
            SequenceWorker.stage4_rf = Result.Working;
            SequenceWorker.stage5_seen = 0;
            SequenceWorker.stage6_rf = Result.Working;
            SequenceWorker.stage7_seen = 0;

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            DoCloneBehavior(cloneBehavior, ref state);
            state.Update();

            Assert.AreEqual(1, SequenceWorker.stage1_seen);
            Assert.AreEqual(0, SequenceWorker.stage3_seen);
            Assert.AreEqual(0, SequenceWorker.stage5_seen);
            Assert.AreEqual(0, SequenceWorker.stage7_seen);

            DoCloneBehavior(cloneBehavior, ref state);
            state.Update();

            Assert.AreEqual(1, SequenceWorker.stage1_seen);
            Assert.AreEqual(0, SequenceWorker.stage3_seen);
            Assert.AreEqual(0, SequenceWorker.stage5_seen);
            Assert.AreEqual(0, SequenceWorker.stage7_seen);

            DoCloneBehavior(cloneBehavior, ref state);
            SequenceWorker.stage2_rf = Result.Success;
            state.Update();

            Assert.AreEqual(1, SequenceWorker.stage1_seen);
            Assert.AreEqual(1, SequenceWorker.stage3_seen);
            Assert.AreEqual(0, SequenceWorker.stage5_seen);
            Assert.AreEqual(0, SequenceWorker.stage7_seen);

            DoCloneBehavior(cloneBehavior, ref state);
            SequenceWorker.stage4_rf = Result.Failure;
            state.Update();

            Assert.AreEqual(1, SequenceWorker.stage1_seen);
            Assert.AreEqual(1, SequenceWorker.stage3_seen);
            Assert.AreEqual(0, SequenceWorker.stage5_seen);
            Assert.AreEqual(0, SequenceWorker.stage7_seen);

            DoCloneBehavior(cloneBehavior, ref state);
            state.Update();

            Assert.AreEqual(2, SequenceWorker.stage1_seen);
            Assert.AreEqual(2, SequenceWorker.stage3_seen);
            Assert.AreEqual(0, SequenceWorker.stage5_seen);
            Assert.AreEqual(0, SequenceWorker.stage7_seen);

            DoCloneBehavior(cloneBehavior, ref state);
            SequenceWorker.stage4_rf = Result.Success;
            SequenceWorker.stage6_rf = Result.Success;
            state.Update();

            Assert.AreEqual(3, SequenceWorker.stage1_seen);
            Assert.AreEqual(3, SequenceWorker.stage3_seen);
            Assert.AreEqual(1, SequenceWorker.stage5_seen);
            Assert.AreEqual(1, SequenceWorker.stage7_seen);

            DoCloneBehavior(cloneBehavior, ref state);
        }
    }
}