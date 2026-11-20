using Arbor;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Assert = NUnit.Framework.Assert;

namespace ArborTest
{
    // Carries a local across a yield, so the compiler hoists it into a field of the state machine.
    public partial class SignatureHoisted : Node
    {
        [Dec.RecorderEnumerator.RecordableEnumerable]
        public override IEnumerable<Result> Worker()
        {
            int count = 0;
            while (true)
            {
                count++;
                yield return count > 0 ? Result.Working : Result.Failure;
            }
        }
    }

    // Captures a local in a lambda held across a yield, so the local moves into a display class and the delegate records the lambda's generated method name.
    [Dec.RecorderEnumerator.RecordableClosures]
    public partial class SignatureCaptured : Node
    {
        [Dec.RecorderEnumerator.RecordableEnumerable]
        public override IEnumerable<Result> Worker()
        {
            int captured = 0;
            Func<int> read = () => captured;
            while (true)
            {
                captured++;
                yield return read() > 0 ? Result.Working : Result.Failure;
            }
        }
    }

    // Drives a helper iterator with foreach, so the Worker's state machine holds a second, separately-generated state machine.
    public partial class SignatureHelper : Node
    {
        [Dec.RecorderEnumerator.RecordableEnumerable]
        public override IEnumerable<Result> Worker()
        {
            while (true)
            {
                foreach (var result in Step())
                {
                    yield return result;
                }
            }
        }

        [Dec.RecorderEnumerator.RecordableEnumerable]
        private IEnumerable<Result> Step()
        {
            int step = 0;
            while (true)
            {
                step++;
                yield return step > 0 ? Result.Working : Result.Failure;
            }
        }
    }

    // Inherits SignatureHoisted's Worker unchanged, so it records the same enumerator; only the node type tells them apart.
    public partial class SignatureHoistedSubclass : SignatureHoisted { }

    // Covers how an Arbor.State read from a save copes with a tree (blackboard or Workers) that no longer matches the one it was written with.
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public class Signature : Base
    {
        // Each shape of compiler-generated state a Worker can carry into a save, plus every built-in node.
        public class Factory : TreeDec.ITreeFactory
        {
            public static BlackboardParameter<int> value = BlackboardParameter<int>.Tree("value");

            public Node Create(TreeDec tree)
            {
                tree.BlackboardRegister(value);

                return new Arbor.Sequence(
                    new Arbor.Select(new FunctionFull(() => Result.Working)),
                    new SignatureHoisted(),
                    new SignatureCaptured(),
                    new SignatureHelper(),
                    new FunctionSimple(() => true)
                );
            }
        }

        public class FactoryRebuilt : TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree)
            {
                tree.BlackboardRegister(Factory.value);

                return new Arbor.Sequence(
                    new Arbor.Select(new FunctionFull(() => Result.Failure)),
                    new SignatureHoisted(),
                    new SignatureCaptured(),
                    new SignatureHelper(),
                    new FunctionSimple(() => false)
                );
            }
        }

        public class FactoryNoBlackboard : TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree)
            {
                return new Arbor.Sequence(
                    new Arbor.Select(new FunctionFull(() => Result.Working)),
                    new SignatureHoisted(),
                    new SignatureCaptured(),
                    new SignatureHelper(),
                    new FunctionSimple(() => true)
                );
            }
        }

        public class FactoryOrderA : TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree)
            {
                return new Arbor.Sequence(new SignatureHoisted(), new SignatureHelper());
            }
        }

        public class FactoryOrderB : TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree)
            {
                return new Arbor.Sequence(new SignatureHelper(), new SignatureHoisted());
            }
        }

        public class FactoryOrderASubclass : TreeDec.ITreeFactory
        {
            public Node Create(TreeDec tree)
            {
                return new Arbor.Sequence(new SignatureHoistedSubclass(), new SignatureHelper());
            }
        }

        [SetUp]
        public void Setup()
        {
            UpdateTestParameters(new UnitTestParameters { explicitTypes = new Type[] { typeof(Factory), typeof(FactoryRebuilt), typeof(FactoryNoBlackboard), typeof(FactoryOrderA), typeof(FactoryOrderB), typeof(FactoryOrderASubclass) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Signature.Factory"" />
                    </Arbor.TreeDec>
                    <Arbor.TreeDec decName=""TestRebuilt"">
                        <worker class=""ArborTest.Signature.FactoryRebuilt"" />
                    </Arbor.TreeDec>
                    <Arbor.TreeDec decName=""TestNoBlackboard"">
                        <worker class=""ArborTest.Signature.FactoryNoBlackboard"" />
                    </Arbor.TreeDec>
                    <Arbor.TreeDec decName=""TestOrderA"">
                        <worker class=""ArborTest.Signature.FactoryOrderA"" />
                    </Arbor.TreeDec>
                    <Arbor.TreeDec decName=""TestOrderB"">
                        <worker class=""ArborTest.Signature.FactoryOrderB"" />
                    </Arbor.TreeDec>
                    <Arbor.TreeDec decName=""TestOrderASubclass"">
                        <worker class=""ArborTest.Signature.FactoryOrderASubclass"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();
        }

        private static TreeDec Tree => Dec.Database<TreeDec>.Get("Test");

        // A state with every enumerator mid-flight and a blackboard value that nothing in the tree would ever set again.
        private static State CreateRunningState()
        {
            var state = new State(Tree);
            state.Blackboard().Set(Factory.value, 7);

            // The Sequence never gets past its first child, so the later ones are started by hand.
            state.Update();
            state.Update();
            using (new State.Scope(state))
            {
                for (int nodeId = 0; nodeId < state.nodeEntries.Length; ++nodeId)
                {
                    ref var enumerator = ref state.nodeEntries[nodeId].enumerator;
                    if (enumerator == null)
                    {
                        enumerator = Tree.nodes[nodeId].Worker().GetEnumerator();
                        enumerator.MoveNext();
                    }
                }
            }

            Assert.IsTrue(state.nodeEntries.All(entry => entry.enumerator != null));

            return state;
        }

        private static string WithElement(string serialized, string element, Action<XElement> edit)
        {
            var doc = XDocument.Parse(serialized);
            edit(doc.Descendants(element).Single());
            return doc.ToString();
        }

        private State ReadExpectingOneWarning(string serialized, string expected)
        {
            int warnings = 0;
            State read = null;
            ExpectWarnings(() => read = Dec.Recorder.Read<State>(serialized), msg =>
            {
                warnings++;
                return msg.Contains(expected);
            });
            Assert.AreEqual(1, warnings, "A mismatched state should report exactly once");

            return read;
        }

        // How a save's tree differs from this build's.
        public enum SignatureEdit
        {
            // Written before saves recorded the structure signature, so nothing says what kind of change it was.
            Legacy,

            // Only Worker internals differ: the same nodes and blackboard parameters.
            Workers,

            // The nodes or blackboard parameters differ.
            Structure,
        }

        // A save of a running state, altered as though written by a build whose tree differs in the way `edit` says.
        private static string WriteWithSignatureMismatch(SignatureEdit edit)
        {
            var doc = XDocument.Parse(Dec.Recorder.Write(CreateRunningState()));
            void Bump(string name)
            {
                var element = doc.Descendants(name).Single();
                element.Value = (ulong.Parse(element.Value) + 1).ToString();
            }

            Bump("signature");
            switch (edit)
            {
                case SignatureEdit.Legacy:
                    doc.Descendants("signatureStructure").Single().Remove();
                    break;
                case SignatureEdit.Workers:
                    break;
                case SignatureEdit.Structure:
                    Bump("signatureStructure");
                    break;
            }

            return doc.ToString();
        }

        private static void AssertNodesRestarted(State read)
        {
            Assert.AreEqual(Tree.nodes.Length, read.nodeEntries.Length);
            Assert.IsTrue(read.nodeEntries.All(entry => entry.enumerator == null && entry.debugState == null), "Every node should restart from scratch");
            Assert.IsEmpty(read.active);
            Assert.AreEqual(0, read.debugCurrentFrame);
        }

        // A save that can't vouch for its structure, or whose structure changed, starts over blackboard and all.
        [Test]
        public void Read_StructureMismatch_ResetsFully([Values(SignatureEdit.Legacy, SignatureEdit.Structure)] SignatureEdit edit)
        {
            var read = ReadExpectingOneWarning(WriteWithSignatureMismatch(edit), "signature mismatch;");

            AssertNodesRestarted(read);
            Assert.AreEqual(0, read.Blackboard().Get(Factory.value), "The blackboard goes back to the template along with everything else");
            Assert.AreEqual(State.LoadReset.Full, read.LoadResetConsume());
            Assert.AreEqual(State.LoadReset.None, read.LoadResetConsume(), "Consuming clears it");

            // And it picks up cleanly from the top.
            read.Update();
            read.Update();
            Assert.IsNotNull(read.nodeEntries[0].enumerator);
        }

        // Matching structure with different Worker code (edited locals, another build configuration) restarts the nodes but keeps the saved blackboard.
        [Test]
        public void Read_WorkerMismatch_KeepsBlackboard()
        {
            var read = ReadExpectingOneWarning(WriteWithSignatureMismatch(SignatureEdit.Workers), "Worker code only");

            AssertNodesRestarted(read);
            Assert.AreEqual(7, read.Blackboard().Get(Factory.value), "The blackboard reads back as saved");
            Assert.AreEqual(State.LoadReset.Nodes, read.LoadResetConsume());
            Assert.AreEqual(State.LoadReset.None, read.LoadResetConsume(), "Consuming clears it");

            read.Update();
            read.Update();
            Assert.IsNotNull(read.nodeEntries[0].enumerator);

            // Fully restored, it round-trips like any other state.
            var state = read;
            DoCloneBehavior(CloneBehavior.WriteRead, ref state);
            Assert.AreEqual(7, state.Blackboard().Get(Factory.value));
        }

        // A State a load reset hasn't been recovered by its host yet; copying it reports once, naming the tree, and consuming first keeps it quiet.
        [Test]
        public void Copy_Unrecovered_Errors([Values(SignatureEdit.Legacy, SignatureEdit.Workers)] SignatureEdit edit, [Values(CloneBehavior.Clone, CloneBehavior.WriteRead)] CloneBehavior cloneBehavior)
        {
            void Copy(State state)
            {
                if (cloneBehavior == CloneBehavior.Clone)
                {
                    Dec.Recorder.Clone(state);
                }
                else
                {
                    Dec.Recorder.Write(state);
                }
            }

            var serialized = WriteWithSignatureMismatch(edit);

            var unrecovered = ReadExpectingOneWarning(serialized, "signature mismatch");
            int errors = 0;
            ExpectErrors(() => Copy(unrecovered), msg =>
            {
                errors++;
                return msg.Contains(Tree.DecName) && msg.Contains("before its host recovered it");
            });
            Assert.AreEqual(1, errors);

            // once only, so a held original doesn't report on every later copy
            Copy(unrecovered);
            Assert.AreEqual(State.LoadReset.None, unrecovered.LoadResetConsume());

            var recovered = ReadExpectingOneWarning(serialized, "signature mismatch");
            Assert.AreNotEqual(State.LoadReset.None, recovered.LoadResetConsume());
            Copy(recovered);
        }

        [Test]
        public void LoadReset_NoneUnlessReset()
        {
            var state = CreateRunningState();
            Assert.AreEqual(State.LoadReset.None, state.LoadResetConsume(), "A fresh state wasn't loaded at all");

            DoCloneBehavior(CloneBehavior.WriteRead, ref state);
            Assert.AreEqual(State.LoadReset.None, state.LoadResetConsume(), "A faithful load discards nothing");
        }

        // Saves from before State kept one entry per node recorded three parallel arrays instead: the enumerators and the two debug arrays. Those load without warnings and restart their trees.
        [Test]
        public void Read_OldPerNodeArrays_ResetsCleanly()
        {
            var serialized = WithElement(Dec.Recorder.Write(CreateRunningState()), "nodeEntries", entries =>
            {
                var items = entries.Elements("li").ToList();
                XElement OldArray(string name, string field)
                {
                    return new XElement(name, items.Select(item =>
                    {
                        var value = new XElement(item.Element(field) ?? new XElement(field));
                        value.Name = "li";
                        return value;
                    }));
                }
                entries.AddBeforeSelf(OldArray("enumerators", "enumerator"), OldArray("debugLastStates", "debugState"), OldArray("debugLastStateFrames", "debugFrame"));
                entries.Remove();
            });

            // Outside ExpectWarnings, any warning or error fails the test.
            var read = Dec.Recorder.Read<State>(serialized);

            Assert.AreEqual(Tree.nodes.Length, read.nodeEntries.Length);
            Assert.IsTrue(read.nodeEntries.All(entry => entry.enumerator == null), "Every node should restart from scratch");
            Assert.IsEmpty(read.active);
            Assert.AreEqual(0, read.Blackboard().Get(Factory.value), "The blackboard goes back to the template along with everything else");
            Assert.AreEqual(State.LoadReset.Full, read.LoadResetConsume());

            read.Update();
            Assert.IsNotNull(read.nodeEntries[0].enumerator);
        }

        [Test]
        public void Read_Unchanged_Resumes([Values(CloneBehavior.Clone, CloneBehavior.WriteRead)] CloneBehavior cloneBehavior)
        {
            var state = CreateRunningState();

            // Fails on any checksum difference, so this also shows nothing got reset.
            DoCloneBehavior(cloneBehavior, ref state);

            Assert.IsTrue(state.nodeEntries.All(entry => entry.enumerator != null), "A matching state should resume mid-flight");
            Assert.AreEqual(7, state.Blackboard().Get(Factory.value));
        }

        // Guards against the signature picking up anything instance- or dec-specific (like which lambdas the factory passed in), which would reset every save on every load.
        [Test]
        public void Signature_SameLayout_Matches()
        {
            Assert.AreEqual(Tree.signature, Dec.Database<TreeDec>.Get("TestRebuilt").signature);
        }

        [Test]
        public void SignatureStructure_SameLayout_Matches()
        {
            Assert.AreEqual(Tree.signatureStructure, Dec.Database<TreeDec>.Get("TestRebuilt").signatureStructure);
        }

        [Test]
        public void Signature_Blackboard_Differs()
        {
            Assert.AreNotEqual(Tree.signature, Dec.Database<TreeDec>.Get("TestNoBlackboard").signature);
            Assert.AreNotEqual(Tree.signatureStructure, Dec.Database<TreeDec>.Get("TestNoBlackboard").signatureStructure);
        }

        [Test]
        public void Signature_NodeOrder_Differs()
        {
            Assert.AreNotEqual(Dec.Database<TreeDec>.Get("TestOrderA").signature, Dec.Database<TreeDec>.Get("TestOrderB").signature);
            Assert.AreNotEqual(Dec.Database<TreeDec>.Get("TestOrderA").signatureStructure, Dec.Database<TreeDec>.Get("TestOrderB").signatureStructure);
        }

        // A node swapped for a subclass that inherits its Worker records the same enumerator, so only the structure signature notices, and the load starts over in full.
        [Test]
        public void SignatureStructure_InheritedWorker_ResetsFully()
        {
            var original = Dec.Database<TreeDec>.Get("TestOrderA");
            var swapped = Dec.Database<TreeDec>.Get("TestOrderASubclass");
            Assert.AreEqual(original.signature, swapped.signature);
            Assert.AreNotEqual(original.signatureStructure, swapped.signatureStructure);

            var state = new State(original);
            state.Update();
            var serialized = WithElement(Dec.Recorder.Write(state), "tree", el => el.Value = swapped.DecName);

            var read = ReadExpectingOneWarning(serialized, "signature mismatch;");
            Assert.AreSame(swapped, read.Tree);
            Assert.IsTrue(read.nodeEntries.All(entry => entry.enumerator == null));
            Assert.AreEqual(State.LoadReset.Full, read.LoadResetConsume());
        }

        // Every node type already differs by name; this is what shows the hoisted locals themselves are in there.
        [Test]
        public void NodeLayout_IncludesHoistedLocals()
        {
            Assert.That(TreeDec.NodeLayout(Tree.nodes.OfType<SignatureHoisted>().Single()), Does.Match("count.*5__"));
        }

        // The layout has to be what Dec actually writes, so a live, mid-flight enumerator should record exactly the fields a fresh one reports.
        [Test]
        public void NodeLayout_MatchesRecordedEnumerators()
        {
            var state = CreateRunningState();
            var nodeEntries = Dec.Reflection.Enumerate(state).Children.Single(entry => entry.Label == "nodeEntries");

            for (int nodeId = 0; nodeId < Tree.nodes.Length; ++nodeId)
            {
                var recorded = nodeEntries.Children[nodeId].Children.Single(entry => entry.Label == "enumerator");
                var layout = recorded.Value.GetType() + "\n" + string.Concat(recorded.Children.Select(entry => $"{entry.Label}:{entry.DeclaredType}\n"));

                Assert.AreEqual(TreeDec.NodeLayout(Tree.nodes[nodeId]), layout);
            }
        }
    }
}
