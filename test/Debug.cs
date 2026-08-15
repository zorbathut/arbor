using Arbor;
using NUnit.Framework;
using System.Collections.Generic;
using Assert = NUnit.Framework.Assert;

namespace ArborTest
{
    [TestFixture]
    public partial class Debug : Base
    {
        #region Tree Factories

        // Simple single-node trees
        public class SucceedTree : TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree) => new Succeed();
        }

        public class FailTree : TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree) => new Fail();
        }

        public class WaitTree : TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree) => new WaitNode();
        }

        // Sequence with Succeed then WaitNode
        // nodes[0] = Sequence, nodes[1] = Succeed, nodes[2] = WaitNode
        public class SequenceWaitTree : TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree)
            {
                return new Arbor.Sequence(new Succeed(), new WaitNode());
            }
        }

        // Node that reads its result from a blackboard parameter
        public partial class BlackboardResultNode : Node
        {
            public BlackboardParameter<Result> ResultId;

            [Dec.RecorderEnumerator.RecordableEnumerable]
            public override IEnumerable<Result> Worker()
            {
                while (true)
                {
                    yield return Result;
                }
            }
        }

        // Select with controllable first child - for testing Terminated state
        // nodes[0] = Select, nodes[1] = BlackboardResultNode, nodes[2] = WaitNode
        public class SelectTree : TreeDec.ITreeFactory
        {
            public static BlackboardParameter<Result> FirstChildResult = BlackboardParameter<Result>.Tree("firstChildResult");

            public Node Create(TreeDec tree)
            {
                return new Arbor.Select(
                    new BlackboardResultNode() { ResultId = FirstChildResult },
                    new WaitNode()
                );
            }
        }

        #endregion

        #region NeverExecuted

        [Test]
        public void NeverExecuted_ReturnsNull()
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(WaitTree) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Debug.WaitTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new State(Dec.Database<TreeDec>.Get("Test"));

            // No Update() called - node was never executed
            var node = state.Tree.nodes[0];
            var (nodeState, _) = state.DebugGetNodeState(node);
            Assert.IsNull(nodeState);
        }

        #endregion

        #region Working State

        [Test]
        public void ActiveNode_ReturnsWorking([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(WaitTree) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Debug.WaitTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new State(Dec.Database<TreeDec>.Get("Test"));

            state.Update();
            DoCloneBehavior(cloneBehavior, ref state);

            var node = state.Tree.nodes[0];
            var (nodeState, framesSince) = state.DebugGetNodeState(node);
            Assert.AreEqual(DebugNodeState.Working, nodeState);
            Assert.AreEqual(0, framesSince);
        }

        [Test]
        public void WorkingNode_AlwaysZeroFramesSince([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(WaitTree) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Debug.WaitTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new State(Dec.Database<TreeDec>.Get("Test"));

            // Multiple updates - node stays Working
            state.Update();
            state.Update();
            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);

            var node = state.Tree.nodes[0];
            var (nodeState, framesSince) = state.DebugGetNodeState(node);
            Assert.AreEqual(DebugNodeState.Working, nodeState);
            Assert.AreEqual(0, framesSince); // Always 0 for active nodes
        }

        #endregion

        #region Success State

        [Test]
        public void CompletedSuccess_ReturnsSuccess([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(SucceedTree) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Debug.SucceedTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new State(Dec.Database<TreeDec>.Get("Test"));

            state.Update();
            DoCloneBehavior(cloneBehavior, ref state);

            var node = state.Tree.nodes[0];
            var (nodeState, framesSince) = state.DebugGetNodeState(node);
            Assert.AreEqual(DebugNodeState.Success, nodeState);
            Assert.AreEqual(0, framesSince);
        }

        #endregion

        #region Failure State

        [Test]
        public void CompletedFailure_ReturnsFailure([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(FailTree) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Debug.FailTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new State(Dec.Database<TreeDec>.Get("Test"));

            state.Update();
            DoCloneBehavior(cloneBehavior, ref state);

            var node = state.Tree.nodes[0];
            var (nodeState, framesSince) = state.DebugGetNodeState(node);
            Assert.AreEqual(DebugNodeState.Failure, nodeState);
            Assert.AreEqual(0, framesSince);
        }

        #endregion

        #region Terminated State

        [Test]
        public void ResetWhileActive_SetsTerminated([Values] CloneBehavior cloneBehavior)
        {
            // Use Select(BlackboardResultNode, WaitNode) to control which branch is taken
            // First run: BlackboardResultNode fails → Select tries WaitNode → Working
            // After Reset + path change: BlackboardResultNode succeeds → Select succeeds → WaitNode never runs
            // This allows us to observe Terminated via public API since WaitNode isn't in active list
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(SelectTree), typeof(BlackboardResultNode) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Debug.SelectTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new State(Dec.Database<TreeDec>.Get("Test"));

            // First run: BlackboardResultNode fails, Select tries WaitNode, WaitNode works
            state.Blackboard().Set(SelectTree.FirstChildResult, Result.Failure);
            state.Update();

            // nodes[0] = Select, nodes[1] = BlackboardResultNode, nodes[2] = WaitNode
            var waitNode = state.Tree.nodes[2];
            var (preResetState, _) = state.DebugGetNodeState(waitNode);
            Assert.AreEqual(DebugNodeState.Working, preResetState);

            state.Reset();
            // WaitNode is now Terminated internally (but still in active list)

            DoCloneBehavior(cloneBehavior, ref state);

            // Change path: BlackboardResultNode now succeeds, so Select succeeds without touching WaitNode
            state.Blackboard().Set(SelectTree.FirstChildResult, Result.Success);
            state.Update();
            // active.Clear() runs first, then Select runs with new path
            // WaitNode is NOT executed, NOT added to active list

            // Now we can observe Terminated via public API!
            var (postResetState, framesSince) = state.DebugGetNodeState(state.Tree.nodes[2]);
            Assert.AreEqual(DebugNodeState.Terminated, postResetState);
            Assert.AreEqual(1, framesSince); // Reset was 1 frame ago
        }

        #endregion

        #region Frame Counting

        [Test]
        public void FramesSince_IncrementsProperly([Values] CloneBehavior cloneBehavior)
        {
            // Use Sequence(Succeed, WaitNode) so Succeed completes once and WaitNode keeps running
            // This way Succeed's debug state stays at frame 1 while the tree continues
            // nodes[0] = Sequence, nodes[1] = Succeed, nodes[2] = WaitNode
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(SequenceWaitTree) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Debug.SequenceWaitTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new State(Dec.Database<TreeDec>.Get("Test"));

            state.Update(); // Frame 1 - Succeed succeeds, WaitNode starts working

            var succeedNode = state.Tree.nodes[1];
            var (state0, framesSince0) = state.DebugGetNodeState(succeedNode);
            Assert.AreEqual(DebugNodeState.Success, state0);
            Assert.AreEqual(0, framesSince0);

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update(); // Frame 2 - WaitNode continues working

            var (state1, framesSince1) = state.DebugGetNodeState(state.Tree.nodes[1]);
            Assert.AreEqual(DebugNodeState.Success, state1);
            Assert.AreEqual(1, framesSince1);

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update(); // Frame 3

            var (state2, framesSince2) = state.DebugGetNodeState(state.Tree.nodes[1]);
            Assert.AreEqual(DebugNodeState.Success, state2);
            Assert.AreEqual(2, framesSince2);

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update(); // Frame 4

            var (state3, framesSince3) = state.DebugGetNodeState(state.Tree.nodes[1]);
            Assert.AreEqual(DebugNodeState.Success, state3);
            Assert.AreEqual(3, framesSince3);
        }

        #endregion

        #region Tree Property

        [Test]
        public void TreeProperty_ReturnsCorrectTree()
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(SucceedTree) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Debug.SucceedTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var tree = Dec.Database<TreeDec>.Get("Test");
            var state = new State(tree);

            Assert.AreSame(tree, state.Tree);
        }

        #endregion
    }
}
