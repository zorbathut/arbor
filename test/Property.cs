using Arbor;
using NUnit.Framework;
using System.Collections.Generic;

namespace ArborTest
{
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public partial class Property : Base
    {
        [Dec.StaticReferences]
        public static class PropertyDecs
        {
            static PropertyDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static PropertyDec<int> IntProperty;
            public static PropertyDec<string> StringProperty;
            public static PropertyDec<bool> BoolProperty;
        }

        public partial class PropertyTestNode : Arbor.Node
        {
            [Dec.RecorderEnumerator.RecordableEnumerable]
            public override IEnumerable<Arbor.Result> Worker()
            {
                yield return Arbor.Result.Working;
                yield return Arbor.Result.Success;
            }
        }

        [Test]
        public void Basic([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(PropertyDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.BasePropertyDec decName=""IntProperty"" class=""Arbor.PropertyDec{int}"" />
                    <Arbor.BasePropertyDec decName=""StringProperty"" class=""Arbor.PropertyDec{string}"" />
                    <Arbor.BasePropertyDec decName=""BoolProperty"" class=""Arbor.PropertyDec{bool}"" />
                </Decs>
            ");
            parser.Finish();

            var blackboardGlobal = new Blackboard();
            Arbor.Tree tree = new Arbor.Tree(new PropertyTestNode()
                .PropertyAttach(PropertyDecs.IntProperty, 42)
                .PropertyAttach(PropertyDecs.StringProperty, "Hello")
                .PropertyAttach(PropertyDecs.BoolProperty, true)
            , blackboardGlobal);

            Assert.AreEqual(0, tree.PropertyGet(PropertyDecs.IntProperty));
            Assert.AreEqual(null, tree.PropertyGet(PropertyDecs.StringProperty));
            Assert.AreEqual(false, tree.PropertyGet(PropertyDecs.BoolProperty));

            tree.Update(blackboardGlobal);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            Assert.AreEqual(42, tree.PropertyGet(PropertyDecs.IntProperty));
            Assert.AreEqual("Hello", tree.PropertyGet(PropertyDecs.StringProperty));
            Assert.AreEqual(true, tree.PropertyGet(PropertyDecs.BoolProperty));
        }

        [Test]
        public void PropertyInheritanceTest([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(PropertyDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.BasePropertyDec decName=""IntProperty"" class=""Arbor.PropertyDec{int}"" />
                    <Arbor.BasePropertyDec decName=""StringProperty"" class=""Arbor.PropertyDec{string}"" />
                    <Arbor.BasePropertyDec decName=""BoolProperty"" class=""Arbor.PropertyDec{bool}"" />
                </Decs>
            ");
            parser.Finish();

            var blackboardGlobal = new Blackboard();
            Arbor.Tree tree = new Arbor.Tree(new Arbor.Sequence(
                new PropertyTestNode()
                    .PropertyAttach(PropertyDecs.IntProperty, 10),
                new PropertyTestNode()
                    .PropertyAttach(PropertyDecs.IntProperty, 20),
                new PropertyTestNode()
            ), blackboardGlobal);

            tree.Update(blackboardGlobal);

            Assert.AreEqual(10, tree.PropertyGet(PropertyDecs.IntProperty));

            tree.Update(blackboardGlobal);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            Assert.AreEqual(20, tree.PropertyGet(PropertyDecs.IntProperty));

            tree.Update(blackboardGlobal);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            Assert.AreEqual(0, tree.PropertyGet(PropertyDecs.IntProperty));
        }

        [Test]
        public void PropertyOverrideTest([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(PropertyDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.BasePropertyDec decName=""IntProperty"" class=""Arbor.PropertyDec{int}"" />
                    <Arbor.BasePropertyDec decName=""StringProperty"" class=""Arbor.PropertyDec{string}"" />
                    <Arbor.BasePropertyDec decName=""BoolProperty"" class=""Arbor.PropertyDec{bool}"" />
                </Decs>
            ");
            parser.Finish();

            var blackboardGlobal = new Blackboard();
            Arbor.Tree tree = new Arbor.Tree(new Arbor.Sequence(
                    new PropertyTestNode(),
                    new PropertyTestNode()
                        .PropertyAttach(PropertyDecs.IntProperty, 20),
                    new PropertyTestNode()
                ).PropertyAttach(PropertyDecs.IntProperty, 10)
            , blackboardGlobal);

            tree.Update(blackboardGlobal);

            Assert.AreEqual(10, tree.PropertyGet(PropertyDecs.IntProperty));

            tree.Update(blackboardGlobal);
            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            Assert.AreEqual(20, tree.PropertyGet(PropertyDecs.IntProperty));

            tree.Update(blackboardGlobal);
            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            Assert.AreEqual(10, tree.PropertyGet(PropertyDecs.IntProperty));
        }

        [Test]
        public void PropertyDefaultValueTest([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(PropertyDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.BasePropertyDec decName=""IntProperty"" class=""Arbor.PropertyDec{int}"" />
                    <Arbor.BasePropertyDec decName=""StringProperty"" class=""Arbor.PropertyDec{string}"" />
                    <Arbor.BasePropertyDec decName=""BoolProperty"" class=""Arbor.PropertyDec{bool}"" />
                </Decs>
            ");
            parser.Finish();

            var blackboardGlobal = new Blackboard();
            Arbor.Tree tree = new Arbor.Tree(new PropertyTestNode(), blackboardGlobal);

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);

            tree.Update(blackboardGlobal);

            Assert.AreEqual(0, tree.PropertyGet(PropertyDecs.IntProperty));
            Assert.AreEqual(null, tree.PropertyGet(PropertyDecs.StringProperty));
            Assert.AreEqual(false, tree.PropertyGet(PropertyDecs.BoolProperty));

            DoCloneBehavior(cloneBehavior, ref tree, ref blackboardGlobal);
        }
    }
}