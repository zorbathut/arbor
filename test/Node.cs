
using Arbor;
using NUnit.Framework;
using System.Collections.Generic;

namespace ArborTest
{
    [TestFixture]
    public class NodeTest : Base
    {
        private class TestNode : Arbor.Node
        {
            public override IEnumerable<Result> Worker()
            {
                yield return Result.Success;
            }
        }

        [Test]
        public void InitRunningCheck()
        {
            var node = new TestNode();
            var nodeList = new List<Arbor.Node>();
            var treeDec = new Arbor.TreeDec();

            // Attempt to init node directly without setting initRunning - should error
            ExpectErrors(() => node.Init(treeDec, nodeList), errorValidator: err => err.Contains("Init must be called from within a full tree init; individual nodes cannot be initted independently!"));
        }
    }
}

// this isn't even a test fixture, we're just making sure it compiles
public partial class NamespacelessTestNode : Arbor.Node
{
    public Arbor.BlackboardParameter<int> ParameterId = new Arbor.BlackboardParameter<int>();

    public override IEnumerable<Result> Worker()
    {
        yield return Result.Success;
    }
}
