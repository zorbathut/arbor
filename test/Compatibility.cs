
using Arbor;
using NUnit.Framework;
using System;
using Assert = NUnit.Framework.Assert;

namespace ArborTest
{
    [TestFixture]
    public class Compatibility : Base
    {
        public static class Before
        {
            public class Simple : TreeDec.ITreeFactory
            {
                public static bool stage1_seen = false;

                public Node Create(TreeDec treeDec)
                {
                    return new Arbor.Sequence(
                        new FunctionSimple(() => stage1_seen = true)
                    );
                }
            }
        }

        public static class After
        {
            public class Simple : TreeDec.ITreeFactory
            {
                public static bool stage1_seen = false;
                public static bool stage2_seen = false;

                public Node Create(TreeDec treeDec)
                {
                    return new Arbor.Sequence(
                        new FunctionSimple(() => stage1_seen = true),
                        new FunctionSimple(() => stage2_seen = true)
                    );
                }
            }
        }

        [Test]
        public void Basic()
        {
            string saved;

            {
                UpdateTestParameters(new UnitTestParameters { explicitTypes = new Type[] { typeof(Before.Simple) } });

                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <Arbor.TreeDec decName=""Test"">
                            <worker class=""ArborTest.Compatibility.Before.Simple"" />
                        </Arbor.TreeDec>
                    </Decs>
                ");
                parser.Finish();

                var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

                Before.Simple.stage1_seen = false;

                state.Update();

                Assert.IsTrue(Before.Simple.stage1_seen);

                saved = Dec.Recorder.Write(state);
            }

            Clean();

            {
                UpdateTestParameters(new UnitTestParameters { explicitTypes = new Type[] { typeof(After.Simple) } });

                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <Arbor.TreeDec decName=""Test"">
                            <worker class=""ArborTest.Compatibility.After.Simple"" />
                        </Arbor.TreeDec>
                    </Decs>
                ");
                parser.Finish();

                Arbor.State state = default;
                ExpectWarnings(() => state = Dec.Recorder.Read<Arbor.State>(saved));

                After.Simple.stage1_seen = false;
                After.Simple.stage2_seen = false;

                state.Update();

                Assert.IsTrue(After.Simple.stage1_seen);
                Assert.IsTrue(After.Simple.stage2_seen);
            }
        }
    }
}
