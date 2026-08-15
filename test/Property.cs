using Arbor;
using NUnit.Framework;
using System.Collections.Generic;
using Assert = NUnit.Framework.Assert;

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

        public class BasicTree : Arbor.TreeDec.ITreeFactory
        {
            public Node Create(TreeDec treeDec)
            {
                return new PropertyTestNode()
                    .PropertyAttach(PropertyDecs.IntProperty, 42)
                    .PropertyAttach(PropertyDecs.StringProperty, "Hello")
                    .PropertyAttach(PropertyDecs.BoolProperty, true);
            }
        }

        [Test]
        public void Basic([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(BasicTree) },
                explicitStaticRefs = new System.Type[] { typeof(PropertyDecs) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.BasePropertyDec decName=""IntProperty"" class=""Arbor.PropertyDec{int}"" />
                    <Arbor.BasePropertyDec decName=""StringProperty"" class=""Arbor.PropertyDec{string}"" />
                    <Arbor.BasePropertyDec decName=""BoolProperty"" class=""Arbor.PropertyDec{bool}"" />

                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Property.BasicTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            Assert.AreEqual(0, state.PropertyGet(PropertyDecs.IntProperty));
            Assert.AreEqual(null, state.PropertyGet(PropertyDecs.StringProperty));
            Assert.AreEqual(false, state.PropertyGet(PropertyDecs.BoolProperty));

            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);

            Assert.AreEqual(42, state.PropertyGet(PropertyDecs.IntProperty));
            Assert.AreEqual("Hello", state.PropertyGet(PropertyDecs.StringProperty));
            Assert.AreEqual(true, state.PropertyGet(PropertyDecs.BoolProperty));
        }

        public class PropertyInheritanceTree : Arbor.TreeDec.ITreeFactory
        {
            public Node Create(TreeDec treeDec)
            {
                return new Arbor.Sequence(
                    new PropertyTestNode()
                        .PropertyAttach(PropertyDecs.IntProperty, 10),
                    new PropertyTestNode()
                        .PropertyAttach(PropertyDecs.IntProperty, 20),
                    new PropertyTestNode()
                );
            }
        }

        [Test]
        public void PropertyInheritance([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(PropertyInheritanceTree) },
                explicitStaticRefs = new System.Type[] { typeof(PropertyDecs) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.BasePropertyDec decName=""IntProperty"" class=""Arbor.PropertyDec{int}"" />
                    <Arbor.BasePropertyDec decName=""StringProperty"" class=""Arbor.PropertyDec{string}"" />
                    <Arbor.BasePropertyDec decName=""BoolProperty"" class=""Arbor.PropertyDec{bool}"" />

                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Property.PropertyInheritanceTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            state.Update();

            Assert.AreEqual(10, state.PropertyGet(PropertyDecs.IntProperty));

            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);

            Assert.AreEqual(20, state.PropertyGet(PropertyDecs.IntProperty));

            state.Update();

            DoCloneBehavior(cloneBehavior, ref state);

            Assert.AreEqual(0, state.PropertyGet(PropertyDecs.IntProperty));
        }

        public class PropertyOverrideTree : Arbor.TreeDec.ITreeFactory
        {
            public Node Create(TreeDec treeDec)
            {
                return new Arbor.Sequence(
                    new PropertyTestNode(),
                    new PropertyTestNode()
                        .PropertyAttach(PropertyDecs.IntProperty, 20),
                    new PropertyTestNode()
                ).PropertyAttach(PropertyDecs.IntProperty, 10);
            }
        }

        [Test]
        public void PropertyOverrideTest([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(PropertyOverrideTree) },
                explicitStaticRefs = new System.Type[] { typeof(PropertyDecs) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.BasePropertyDec decName=""IntProperty"" class=""Arbor.PropertyDec{int}"" />
                    <Arbor.BasePropertyDec decName=""StringProperty"" class=""Arbor.PropertyDec{string}"" />
                    <Arbor.BasePropertyDec decName=""BoolProperty"" class=""Arbor.PropertyDec{bool}"" />

                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Property.PropertyOverrideTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            state.Update();

            Assert.AreEqual(10, state.PropertyGet(PropertyDecs.IntProperty));

            state.Update();
            DoCloneBehavior(cloneBehavior, ref state);

            Assert.AreEqual(20, state.PropertyGet(PropertyDecs.IntProperty));

            state.Update();
            DoCloneBehavior(cloneBehavior, ref state);

            Assert.AreEqual(10, state.PropertyGet(PropertyDecs.IntProperty));
        }


        public class PropertyDefaultValueTree : Arbor.TreeDec.ITreeFactory
        {
            public Node Create(TreeDec treeDec)
            {
                return new PropertyTestNode();
            }
        }

        [Test]
        public void PropertyDefaultValue([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new UnitTestParameters {
                explicitTypes = new System.Type[] { typeof(PropertyDefaultValueTree) },
                explicitStaticRefs = new System.Type[] { typeof(PropertyDecs) }
            });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.BasePropertyDec decName=""IntProperty"" class=""Arbor.PropertyDec{int}"" />
                    <Arbor.BasePropertyDec decName=""StringProperty"" class=""Arbor.PropertyDec{string}"" />
                    <Arbor.BasePropertyDec decName=""BoolProperty"" class=""Arbor.PropertyDec{bool}"" />

                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Property.PropertyDefaultValueTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            DoCloneBehavior(cloneBehavior, ref state);

            state.Update();

            Assert.AreEqual(0, state.PropertyGet(PropertyDecs.IntProperty));
            Assert.AreEqual(null, state.PropertyGet(PropertyDecs.StringProperty));
            Assert.AreEqual(false, state.PropertyGet(PropertyDecs.BoolProperty));

            DoCloneBehavior(cloneBehavior, ref state);
        }
    }
}
