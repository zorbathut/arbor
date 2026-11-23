using Arbor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Assert = NUnit.Framework.Assert;

namespace ArborTest
{
    // A blackboard parameter a recorder reads back is known by its label alone, and becomes its tree's parameter of that label as the tree builds: the form a tree takes when it is data rather than code.
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public partial class BlackboardLabel : Base
    {
        public partial class WriteNode : Node, Dec.IRecordable
        {
            public BlackboardParameter<int> ValueId;
            public int amount;

            private WriteNode() { }  // exists just for Dec
            public WriteNode(BlackboardParameter<int> value, int amount)
            {
                ValueId = value;
                this.amount = amount;
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref ValueId, "value");
                recorder.Record(ref amount, nameof(amount));
            }

            [Dec.RecorderEnumerator.RecordableEnumerable]
            public override IEnumerable<Result> Worker()
            {
                Value = amount;
                yield return Result.Success;
            }
        }

        public partial class TextNode : Node, Dec.IRecordable
        {
            public BlackboardParameter<string> TextId;

            private TextNode() { }  // exists just for Dec
            public TextNode(BlackboardParameter<string> text)
            {
                TextId = text;
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref TextId, "text");
            }

            [Dec.RecorderEnumerator.RecordableEnumerable]
            public override IEnumerable<Result> Worker()
            {
                yield return Result.Success;
            }
        }

        // Holds its parameter in a Worker local across a yield, so a save of its State records the parameter itself.
        public partial class LocalNode : Node, Dec.IRecordable
        {
            public BlackboardParameter<int> ValueId;

            private LocalNode() { }  // exists just for Dec
            public LocalNode(BlackboardParameter<int> value)
            {
                ValueId = value;
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref ValueId, "value");
            }

            [Dec.RecorderEnumerator.RecordableEnumerable]
            public override IEnumerable<Result> Worker()
            {
                var local = ValueId;
                yield return Result.Working;
                local.Set(7);
                yield return Result.Success;
            }
        }

        // Mints its parameter in a field initializer, which a read lands on top of.
        public partial class InitializedNode : Node, Dec.IRecordable
        {
            public BlackboardParameter<int> ValueId = BlackboardParameter<int>.Tree("initializer");

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref ValueId, "value");
            }

            public override IEnumerable<Result> Worker()
            {
                yield return Result.Success;
            }
        }

        // Builds whichever root the test left under the tree's name.
        public class PendingTree : TreeDec.ITreeFactory
        {
            public static Dictionary<string, Node> roots = new Dictionary<string, Node>();

            public Node Create(TreeDec treeDec)
            {
                return roots[treeDec.DecName];
            }
        }

        private static T RoundTrip<T>(T node)
        {
            return Dec.Recorder.Read<T>(Dec.Recorder.Write(node));
        }

        private void Build(params (string name, Node root)[] trees)
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(PendingTree) }
            });

            PendingTree.roots = trees.ToDictionary(tree => tree.name, tree => tree.root);
            try
            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, "<Decs>" + string.Concat(trees.Select(tree => $@"
                    <Arbor.TreeDec decName=""{tree.name}"">
                        <worker class=""ArborTest.BlackboardLabel.PendingTree"" />
                    </Arbor.TreeDec>")) + "</Decs>");
                parser.Finish();
            }
            finally
            {
                PendingTree.roots.Clear();
            }
        }

        [Test]
        public void Record_KeyedWritesKeyOnly_ConstantWritesConstant()
        {
            var keyed = Dec.Recorder.Write(new WriteNode(BlackboardParameter<int>.Tree("height"), 10));
            Assert.That(keyed, Does.Contain("<key>height</key>"));
            Assert.That(keyed, Does.Not.Contain("<constant>"));

            var constant = Dec.Recorder.Write(new WriteNode(BlackboardParameter<int>.Constant(3), 10));
            Assert.That(constant, Does.Contain("<constant>3</constant>"));
            Assert.That(constant, Does.Not.Contain("<key>"));
        }

        [Test]
        public void Record_RoundTrip_KeepsLabelAndConstant()
        {
            var keyed = RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("height"), 10)).ValueId;
            Assert.IsFalse(keyed.IsConstant());
            Assert.IsFalse(keyed.IsResolved());
            Assert.AreEqual("[height]", keyed.ToString());

            var constant = RoundTrip(new WriteNode(BlackboardParameter<int>.Constant(3), 10)).ValueId;
            Assert.IsTrue(constant.IsConstant());
            Assert.AreEqual(3, constant.Get());
        }

        // The record is the whole parameter, so one that gives only a constant reads back as that constant, whatever the field initializer minted; a hand-written record leaves the key out rather than writing it null.
        [Test]
        public void Record_ReadOverInitializer_TakesTheRecord()
        {
            var constant = Dec.Recorder.Read<InitializedNode>("<Record><recordFormatVersion>1</recordFormatVersion><data><value><constant>3</constant></value></data></Record>").ValueId;
            Assert.IsTrue(constant.IsConstant());
            Assert.AreEqual(3, constant.Get());

            var keyed = Dec.Recorder.Read<InitializedNode>(Dec.Recorder.Write(new InitializedNode { ValueId = BlackboardParameter<int>.Tree("height") })).ValueId;
            Assert.AreEqual("[height]", keyed.ToString());
            Assert.IsFalse(keyed.IsResolved());
        }

        // Two nodes naming one label share one parameter of the tree: what one writes the other reads.
        [Test]
        public void Resolve_SameLabel_SharesOneParameter([Values] CloneBehavior cloneBehavior)
        {
            var first = RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("height"), 10));
            var second = RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("height"), 20));
            var other = RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("depth"), 30));
            Build(("Test", new Arbor.Sequence(first, second, other)));

            Assert.IsTrue(first.ValueId.IsResolved());
            Assert.AreEqual(first.ValueId.identifier.uid, second.ValueId.identifier.uid);
            Assert.AreNotEqual(first.ValueId.identifier.uid, other.ValueId.identifier.uid);

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));
            state.Update();
            DoCloneBehavior(cloneBehavior, ref state);

            using (new Arbor.State.Scope(state))
            {
                Assert.AreEqual(20, first.ValueId.Get());
                Assert.AreEqual(30, other.ValueId.Get());

                var labels = state.Blackboard().GetAll_Debug().Select(pair => pair.Key).OrderBy(label => label).ToArray();
                Assert.AreEqual(new[] { "depth", "height" }, labels);
            }
        }

        // A rebuild mints a label's parameter a fresh uid, and a save of the old build still reads back into it.
        [Test]
        public void Save_AcrossRebuild_KeepsLabelParameters()
        {
            WriteNode height = null;
            WriteNode depth = null;
            void BuildFresh()
            {
                height = RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("height"), 10));
                depth = RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("depth"), 30));
                Build(("Test", new Arbor.Sequence(height, depth)));
            }

            BuildFresh();
            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));
            state.Update();
            var saved = Dec.Recorder.Write(state);
            var oldUid = height.ValueId.identifier.uid;

            Dec.Database.Clear();
            BuildFresh();
            Assert.AreNotEqual(oldUid, height.ValueId.identifier.uid);

            state = Dec.Recorder.Read<Arbor.State>(saved);
            using (new Arbor.State.Scope(state))
            {
                Assert.AreEqual(10, height.ValueId.Get());
                Assert.AreEqual(30, depth.ValueId.Get());
            }
        }

        // A tree read from a recorder is the same tree as one built in code, down to its signatures, so a save of either loads into the other.
        [Test]
        public void Resolve_MatchesCodeBuiltSignature()
        {
            var height = BlackboardParameter<int>.Tree("height");
            Build(
                ("Code", new Arbor.Sequence(new WriteNode(height, 10), new WriteNode(height, 20))),
                ("Recorded", new Arbor.Sequence(RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("height"), 10)), RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("height"), 20)))));

            var code = Dec.Database<Arbor.TreeDec>.Get("Code");
            var recorded = Dec.Database<Arbor.TreeDec>.Get("Recorded");
            Assert.AreEqual(code.signature, recorded.signature);
            Assert.AreEqual(code.signatureStructure, recorded.signatureStructure);
        }

        // Code shares a parameter by handing one value around, so two it mints under one label are two parameters, as they always were.
        [Test]
        public void Register_CodeMintedSameLabel_StaysTwoParameters()
        {
            Build(("Test", new Arbor.Sequence(new WriteNode(BlackboardParameter<int>.Tree("height"), 10), new WriteNode(BlackboardParameter<int>.Tree("height"), 20))));

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));
            using (new Arbor.State.Scope(state))
            {
                Assert.AreEqual(2, state.Blackboard().GetAll_Debug().Count());
            }
        }

        [Test]
        public void Resolve_SameLabelTwoTypes_Errs()
        {
            var number = RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("thing"), 10));
            var text = RoundTrip(new TextNode(BlackboardParameter<string>.Tree("thing")));

            ExpectErrors(() => Build(("Test", new Arbor.Sequence(number, text))), error => error.Contains("thing") && error.Contains("elsewhere"));
        }

        // A parameter code mints and holds is out of a bare label's reach, so a label of the same name in the same tree is an error whichever registers first.
        [Test]
        public void Resolve_LabelCollidesWithCodeParameter_Errs([Values] bool labelFirst)
        {
            var bare = RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("height"), 10));
            var minted = new WriteNode(BlackboardParameter<int>.Tree("height"), 20);
            var sequence = labelFirst ? new Arbor.Sequence(bare, minted) : new Arbor.Sequence(minted, bare);

            ExpectErrors(() => Build(("Test", sequence)), error => error.Contains("height"));
        }

        // A parameter only a read has seen belongs to no tree yet.
        [Test]
        public void Get_Unresolved_Errs()
        {
            var parameter = RoundTrip(new WriteNode(BlackboardParameter<int>.Tree("height"), 10)).ValueId;
            Build(("Test", new Succeed()));

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));
            using (new Arbor.State.Scope(state))
            {
                ExpectErrors(() => parameter.Get(), error => error.Contains("resolved"));
                ExpectErrors(() => parameter.Set(4), error => error.Contains("resolved"));
            }
        }

        // A State's save records a parameter it holds as its tree's registration index, whichever way the tree came by the parameter.
        [Test]
        public void Save_WorkerLocal_KeepsParameter([Values] CloneBehavior cloneBehavior, [Values] bool recorded)
        {
            var node = new LocalNode(BlackboardParameter<int>.Tree("height"));
            if (recorded)
            {
                node = RoundTrip(node);
            }
            Build(("Test", node));

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));
            state.Update();
            DoCloneBehavior(cloneBehavior, ref state);
            state.Update();

            using (new Arbor.State.Scope(state))
            {
                Assert.AreEqual(7, node.ValueId.Get());
            }
        }

        // The index survives a rebuild that mints every parameter a fresh uid, mid-Worker.
        [Test]
        public void Save_WorkerLocalAcrossRebuild_KeepsParameter([Values] bool recorded)
        {
            LocalNode node = null;
            void BuildFresh()
            {
                node = new LocalNode(BlackboardParameter<int>.Tree("height"));
                if (recorded)
                {
                    node = RoundTrip(node);
                }
                Build(("Test", node));
            }

            BuildFresh();
            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));
            state.Update();
            var saved = Dec.Recorder.Write(state);

            Dec.Database.Clear();
            BuildFresh();

            state = Dec.Recorder.Read<Arbor.State>(saved);
            state.Update();
            using (new Arbor.State.Scope(state))
            {
                Assert.AreEqual(7, node.ValueId.Get());
            }
        }

        // The index form means nothing without the State whose tree it indexes.
        [Test]
        public void Save_IndexOutsideState_Errs()
        {
            var parameter = default(BlackboardParameter<int>);
            ExpectErrors(() => parameter = Dec.Recorder.Read<BlackboardParameter<int>>("<Record><recordFormatVersion>1</recordFormatVersion><data><index>0</index></data></Record>"), error => error.Contains("index"));
            Assert.IsTrue(parameter.IsConstant());
        }

        // An empty label is what a constant looks like.
        [Test]
        public void Tree_EmptyLabel_Errs([Values(null, "")] string label)
        {
            BlackboardParameter<int> parameter = default;
            ExpectErrors(() => parameter = BlackboardParameter<int>.Tree(label), error => error.Contains("empty label"));
            Assert.IsTrue(parameter.IsConstant());
        }
    }
}
