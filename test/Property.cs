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

            Arbor.Tree tree = new Arbor.Tree(new PropertyTestNode()
                .PropertyAttach(PropertyDecs.IntProperty, 42)
                .PropertyAttach(PropertyDecs.StringProperty, "Hello")
                .PropertyAttach(PropertyDecs.BoolProperty, true));

            Assert.AreEqual(0, tree.PropertyGet(PropertyDecs.IntProperty));
            Assert.AreEqual(null, tree.PropertyGet(PropertyDecs.StringProperty));
            Assert.AreEqual(false, tree.PropertyGet(PropertyDecs.BoolProperty));

            tree.Update();

            DoCloneBehavior(cloneBehavior, ref tree);

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

            Arbor.Tree tree = new Arbor.Tree(new Arbor.Sequence(
                new PropertyTestNode()
                    .PropertyAttach(PropertyDecs.IntProperty, 10),
                new PropertyTestNode()
                    .PropertyAttach(PropertyDecs.IntProperty, 20),
                new PropertyTestNode()
            ));

            tree.Update();

            Assert.AreEqual(10, tree.PropertyGet(PropertyDecs.IntProperty));

            tree.Update();

            DoCloneBehavior(cloneBehavior, ref tree);

            Assert.AreEqual(20, tree.PropertyGet(PropertyDecs.IntProperty));

            tree.Update();

            DoCloneBehavior(cloneBehavior, ref tree);

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

            Arbor.Tree tree = new Arbor.Tree(new Arbor.Sequence(
                    new PropertyTestNode(),
                    new PropertyTestNode()
                        .PropertyAttach(PropertyDecs.IntProperty, 20),
                    new PropertyTestNode()
                ).PropertyAttach(PropertyDecs.IntProperty, 10));

            tree.Update();

            Assert.AreEqual(10, tree.PropertyGet(PropertyDecs.IntProperty));

            tree.Update();
            DoCloneBehavior(cloneBehavior, ref tree);

            Assert.AreEqual(20, tree.PropertyGet(PropertyDecs.IntProperty));

            tree.Update();
            DoCloneBehavior(cloneBehavior, ref tree);

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

            Arbor.Tree tree = new Arbor.Tree(new PropertyTestNode());

            DoCloneBehavior(cloneBehavior, ref tree);

            tree.Update();

            Assert.AreEqual(0, tree.PropertyGet(PropertyDecs.IntProperty));
            Assert.AreEqual(null, tree.PropertyGet(PropertyDecs.StringProperty));
            Assert.AreEqual(false, tree.PropertyGet(PropertyDecs.BoolProperty));

            DoCloneBehavior(cloneBehavior, ref tree);
        }
    }
}