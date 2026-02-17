using Arbor;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace ArborTest
{
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public class MemberRegistration : Base
    {
        public class SequenceTree : TreeDec.ITreeFactory
        {
            public Node Create(TreeDec treeDec)
            {
                return new Arbor.Sequence(
                    new WaitNode(),
                    new WaitNode(),
                    new WaitNode()
                );
            }
        }

        [Test]
        public void CloneResetChecksum()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new System.Type[] { typeof(SequenceTree) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.MemberRegistration.SequenceTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var tree = Dec.Database<Arbor.TreeDec>.Get("Test");

            // Create a pair - both enumerators will share the same
            // m_children array from the Sequence node
            var stateA = new Arbor.State(tree);
            var stateB = new Arbor.State(tree);

            stateA.Update();
            stateB.Update();

            // Verify the pair checksums
            Dec.Recorder.ChecksumDiff(
                (stateA, stateB),
                (stateA, stateB),
                Assert.Fail
            );

            // Clone the pair as a unit - the Recorder should see that
            // both enumerators reference the same m_children array.
            // But if it duplicates the array, the sharing topology will be broken in the clone.
            var (cloneA, cloneB) = Dec.Recorder.Clone((stateA, stateB));

            // Verify the cloned pair matches the original pair
            Dec.Recorder.ChecksumDiff(
                (stateA, stateB),
                (cloneA, cloneB),
                Assert.Fail
            );

            // Reset one from each pair, then update
            stateA.Reset();
            cloneA.Reset();

            stateA.Update();
            cloneA.Update();

            // Structurally equivalent - but if m_children sharing was
            // broken by the clone, the checksums will differ.
            Dec.Recorder.ChecksumDiff(
                (stateA, stateB),
                (cloneA, cloneB),
                Assert.Fail
            );
        }
    }
}
