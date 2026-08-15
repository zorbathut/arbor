
using System;
using Arbor;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace ArborTest
{
    /// <summary>
    /// Serialization tests for all base Arbor node types.
    /// Each node is exercised in a Working state (where possible) and then serialized
    /// via Clone and WriteRead to verify that enumerator state survives round-trips.
    /// </summary>
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public class NodeSerialization : Base
    {
        // ---- Select ----

        public class SelectFactory : TreeDec.ITreeFactory
        {
            public static Result child1Result = Result.Working;
            public static Result child2Result = Result.Working;

            public Node Create(TreeDec treeDec)
            {
                return new Select(
                    new ResultFunction(() => child1Result),
                    new ResultFunction(() => child2Result)
                );
            }
        }

        [Test]
        public void SelectWorking([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters { explicitTypes = new Type[] { typeof(SelectFactory) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""SelectTest"">
                        <worker class=""ArborTest.NodeSerialization.SelectFactory"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            SelectFactory.child1Result = Result.Working;
            SelectFactory.child2Result = Result.Working;

            var state = new State(Dec.Database<TreeDec>.Get("SelectTest"));

            // First update: child1 returns Working, Select is Working
            state.Update();

            // Serialize while Select's enumerator is active (foreach captures m_children)
            DoCloneBehavior(cloneBehavior, ref state);

            // child1 fails, Select moves to child2 which is Working
            SelectFactory.child1Result = Result.Failure;
            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);

            // child2 succeeds, Select succeeds
            SelectFactory.child2Result = Result.Success;
            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);
        }

        // ---- Sequence ----

        public class SequenceFactory : TreeDec.ITreeFactory
        {
            public static Result child1Result = Result.Working;
            public static Result child2Result = Result.Working;

            public Node Create(TreeDec treeDec)
            {
                return new Arbor.Sequence(
                    new ResultFunction(() => child1Result),
                    new ResultFunction(() => child2Result)
                );
            }
        }

        [Test]
        public void SequenceWorking([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters { explicitTypes = new Type[] { typeof(SequenceFactory) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""SequenceTest"">
                        <worker class=""ArborTest.NodeSerialization.SequenceFactory"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            SequenceFactory.child1Result = Result.Working;
            SequenceFactory.child2Result = Result.Working;

            var state = new State(Dec.Database<TreeDec>.Get("SequenceTest"));

            // First update: child1 returns Working, Sequence is Working
            state.Update();

            // Serialize while Sequence's enumerator is active
            DoCloneBehavior(cloneBehavior, ref state);

            // child1 succeeds, Sequence moves to child2 which is Working
            SequenceFactory.child1Result = Result.Success;
            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);

            // child2 succeeds, Sequence succeeds
            SequenceFactory.child2Result = Result.Success;
            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);
        }

        // ---- FunctionFull ----

        public class FunctionFullFactory : TreeDec.ITreeFactory
        {
            public static Result funcResult = Result.Working;

            public Node Create(TreeDec treeDec)
            {
                return new FunctionFull(() => funcResult);
            }
        }

        [Test]
        public void FunctionFullWorking([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters { explicitTypes = new Type[] { typeof(FunctionFullFactory) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""FunctionFullTest"">
                        <worker class=""ArborTest.NodeSerialization.FunctionFullFactory"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            FunctionFullFactory.funcResult = Result.Working;

            var state = new State(Dec.Database<TreeDec>.Get("FunctionFullTest"));

            // func returns Working
            state.Update();

            // Serialize while FunctionFull's enumerator is active
            DoCloneBehavior(cloneBehavior, ref state);

            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);

            // func returns Success
            FunctionFullFactory.funcResult = Result.Success;
            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);
        }

        // ---- FunctionSimple ----
        // FunctionSimple always yields Success or Failure (never Working),
        // so its enumerator is never active across updates.
        // We wrap it in a Sequence with a WaitNode to keep the tree in Working state,
        // verifying that FunctionSimple nodes in the tree don't break serialization.

        public class FunctionSimpleFactory : TreeDec.ITreeFactory
        {
            public static bool conditionResult = true;

            public Node Create(TreeDec treeDec)
            {
                return new Arbor.Sequence(
                    new FunctionSimple(() => conditionResult),
                    new WaitNode()
                );
            }
        }

        [Test]
        public void FunctionSimpleInTree([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters { explicitTypes = new Type[] { typeof(FunctionSimpleFactory) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""FunctionSimpleTest"">
                        <worker class=""ArborTest.NodeSerialization.FunctionSimpleFactory"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            FunctionSimpleFactory.conditionResult = true;

            var state = new State(Dec.Database<TreeDec>.Get("FunctionSimpleTest"));

            // FunctionSimple succeeds immediately, WaitNode keeps tree in Working state
            state.Update();

            // Serialize - FunctionSimple's enumerator is already done,
            // but the tree state (with FunctionSimple as a node) must serialize cleanly
            DoCloneBehavior(cloneBehavior, ref state);

            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);
        }
    }
}
