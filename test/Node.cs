
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
            var blackboard = new Blackboard();
            var nodeList = new List<Arbor.Node>();

            // Attempt to init node directly without setting initRunning - should error
            ExpectErrors(() => node.Init(blackboard, nodeList), errorValidator: err => err.Contains("Init must be called from within a full tree init; individual nodes cannot be initted independently!"));
        }
    }
}