using Arbor;
using NUnit.Framework;
using System.Collections.Generic;
using Assert = NUnit.Framework.Assert;

namespace ArborTest
{
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public partial class Event : Base
    {
        public partial class IdleNode : Arbor.Node
        {
            [Dec.RecorderEnumerator.RecordableEnumerable]
            public override IEnumerable<Arbor.Result> Worker()
            {
                yield return Arbor.Result.Working;
            }
        }

        [Dec.StaticReferences]
        public static class EventDecs
        {
            static EventDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EventDec ZeroParameter;
            public static EventDec<int> OneParameter;
            public static EventDec<int, int> TwoParameter;
            public static EventDec<int, int, int> ThreeParameter;
            public static EventDec<int, int, int, int> FourParameter;
        }

        public class Payload : Dec.IRecordable
        {
            public int seen0;
            public int seen1;
            public int seen2;
            public int seen3;
            public int seen4;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref seen0, nameof(seen0));
                recorder.Record(ref seen1, nameof(seen1));
                recorder.Record(ref seen2, nameof(seen2));
                recorder.Record(ref seen3, nameof(seen3));
                recorder.Record(ref seen4, nameof(seen4));
            }
        }

        public class BasicTree : Arbor.TreeDec.ITreeFactory
        {
            public static Payload payload;

            public Node Create(TreeDec tree)
            {
                return new IdleNode()
                    .EventAttach(EventDecs.ZeroParameter, () => payload.seen0++)
                    .EventAttach(EventDecs.OneParameter, (int a) => payload.seen1 += a)
                    .EventAttach(EventDecs.TwoParameter, (int a, int b) => payload.seen2 += a + b)
                    .EventAttach(EventDecs.ThreeParameter, (int a, int b, int c) => payload.seen3 += a + b + c)
                    .EventAttach(EventDecs.FourParameter, (int a, int b, int c, int d) => payload.seen4 += a + b + c + d);
            }
        }

        [Test]
        public void Basic([Values] CloneBehavior cloneBehavior)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new System.Type[] { typeof(BasicTree) }, explicitStaticRefs = new System.Type[] { typeof(EventDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Arbor.BaseEventDec decName=""ZeroParameter"" class=""Arbor.EventDec"" />
                    <Arbor.BaseEventDec decName=""OneParameter"" class=""Arbor.EventDec{int}"" />
                    <Arbor.BaseEventDec decName=""TwoParameter"" class=""Arbor.EventDec{int, int}"" />
                    <Arbor.BaseEventDec decName=""ThreeParameter"" class=""Arbor.EventDec{int, int, int}"" />
                    <Arbor.BaseEventDec decName=""FourParameter"" class=""Arbor.EventDec{int, int, int, int}"" />

                    <Arbor.TreeDec decName=""Test"">
                        <worker class=""ArborTest.Event.BasicTree"" />
                    </Arbor.TreeDec>
                </Decs>
            ");
            parser.Finish();

            BasicTree.payload = new Payload();

            var state = new Arbor.State(Dec.Database<Arbor.TreeDec>.Get("Test"));

            // first-frame events get ignored because nothing has run
            state.Update();

            state.EventInvoke(EventDecs.ZeroParameter);
            state.EventInvoke(EventDecs.OneParameter, 1);
            state.EventInvoke(EventDecs.TwoParameter, 2, 3);
            state.EventInvoke(EventDecs.ThreeParameter, 4, 5, 6);
            state.EventInvoke(EventDecs.FourParameter, 7, 8, 9, 10);

            Assert.AreEqual(1, BasicTree.payload.seen0);
            Assert.AreEqual(1, BasicTree.payload.seen1);
            Assert.AreEqual(5, BasicTree.payload.seen2);
            Assert.AreEqual(15, BasicTree.payload.seen3);
            Assert.AreEqual(34, BasicTree.payload.seen4);

            DoCloneBehavior(cloneBehavior, ref state, ref BasicTree.payload);

            state.EventInvoke(EventDecs.ZeroParameter);
            state.EventInvoke(EventDecs.OneParameter, 11);
            state.EventInvoke(EventDecs.TwoParameter, 12, 13);
            state.EventInvoke(EventDecs.ThreeParameter, 14, 15, 16);
            state.EventInvoke(EventDecs.FourParameter, 17, 18, 19, 20);

            Assert.AreEqual(2, BasicTree.payload.seen0);
            Assert.AreEqual(12, BasicTree.payload.seen1);
            Assert.AreEqual(30, BasicTree.payload.seen2);
            Assert.AreEqual(60, BasicTree.payload.seen3);
            Assert.AreEqual(108, BasicTree.payload.seen4);
        }
    }
}
