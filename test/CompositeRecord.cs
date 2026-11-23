using Arbor;
using NUnit.Framework;
using System.Linq;
using Assert = NUnit.Framework.Assert;

namespace ArborTest
{
    [TestFixture]
    public class CompositeRecord : Base
    {
        // The stock composites record their children and nothing of Node's own bookkeeping, so a recorded tree builds and runs like one built in code.
        [Test]
        public void Record_Composite_KeepsChildren([Values] bool select)
        {
            var first = new BlackboardLabel.WriteNode(BlackboardParameter<int>.Tree("height"), 10);
            var second = new BlackboardLabel.WriteNode(BlackboardParameter<int>.Tree("height"), 20);
            Node composite = select ? new Arbor.Select(first, second) : new Arbor.Sequence(first, second);

            var written = Dec.Recorder.Write(composite);
            Assert.That(written, Does.Not.Contain("nodeIndex"));

            var root = Dec.Recorder.Read<Node>(written);
            Assert.AreEqual(composite.GetType(), root.GetType());
            BlackboardLabel.Build(("Test", root));

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));
            state.Update();
            using (new Arbor.State.Scope(state))
            {
                // read back as bare labels, the two children share one parameter; a Select stops at the first child to succeed, a Sequence runs them all
                Assert.AreEqual(select ? 10 : 20, state.Blackboard().GetAll_Debug().Single().Value);
            }
        }

        // Children are shared references, so one node under two parents reads back as one node, which the build refuses rather than building two copies.
        [Test]
        public void Record_SharedChild_StaysOneNode()
        {
            var child = new BlackboardLabel.WriteNode(BlackboardParameter<int>.Tree("height"), 10);
            var root = Dec.Recorder.Read<Arbor.Sequence>(Dec.Recorder.Write(new Arbor.Sequence(new Arbor.Sequence(child), new Arbor.Select(child))));

            ExpectErrors(() => BlackboardLabel.Build(("Test", root)));
        }
    }
}
