namespace ArborTest
{
    using NUnit.Framework;
    using System;
    using System.Reflection;

    [TestFixture]
    public class Base
    {
        [SetUp] [TearDown]
        public void Clean()
        {
            // we turn on error handling so that global-state resets can work even if we're in the wrong mode
            handlingErrors = true;

            Dec.Database.Clear();

            handlingWarnings = false;
            handledWarning = false;

            handlingErrors = false;
            handledError = false;
        }

        private bool handlingWarnings = false;
        private bool handledWarning = false;

        private bool handlingErrors = false;
        private bool handledError = false;

        [OneTimeSetUp]
        public void PrepHooks()
        {
            Arbor.Config.WarningHandler = str => {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);

                if (handlingWarnings)
                {
                    handledWarning = true;
                }
                else
                {
                    // Throw if we're not handling it - this way we get test failures
                    throw new ArgumentException(str);
                }
            };
            Dec.Config.WarningHandler = Arbor.Config.WarningHandler;

            Arbor.Config.ErrorHandler = str => {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);

                if (handlingErrors)
                {
                    // If we're handling it, don't throw - this way we can validate that fallback behavior is working right
                    handledError = true;
                }
                else
                {
                    // Throw if we're not handling it - this way we get test failures and can validate that exception-passing behavior is working right
                    throw new ArgumentException(str);
                }
            };
            Dec.Config.ErrorHandler = Arbor.Config.ErrorHandler;

            Arbor.Config.ExceptionHandler = e => {
                Arbor.Config.ErrorHandler(e.ToString());
            };
            Dec.Config.ExceptionHandler = Arbor.Config.ExceptionHandler;

            Dec.Config.ConverterFactory = Dec.RecorderEnumerator.Config.ConverterFactory;
        }

        public static void UpdateTestParameters(Dec.Config.UnitTestParameters parameters)
        {
            typeof(Dec.Config).GetField("TestParameters", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, parameters);
        }

        public Arbor.TreeDec CreateDec(Arbor.Node node)
        {
            var tree = Dec.Database.Create<Arbor.TreeDec>("Test");
            tree.root = node;
            tree.PostLoad(Arbor.Config.ErrorHandler);
            return tree;
        }

        protected void ExpectWarnings(Action action)
        {
            Assert.IsFalse(handlingWarnings);
            handlingWarnings = true;
            handledWarning = false;

            action();

            Assert.IsTrue(handlingWarnings);
            Assert.IsTrue(handledWarning);
            handlingWarnings = false;
            handledWarning = false;
        }

        protected void ExpectErrors(Action action)
        {
            Assert.IsFalse(handlingErrors);
            handlingErrors = true;
            handledError = false;

            action();

            Assert.IsTrue(handlingErrors);
            Assert.IsTrue(handledError);
            handlingErrors = false;
            handledError = false;
        }

        public enum CloneBehavior
        {
            Nop,
            Clone,
            WriteRead,
        }

        public void DoCloneBehavior(CloneBehavior cloneBehavior, ref Arbor.TreeInstance treeInstance, ref Arbor.Blackboard globalBlackboard)
        {
            object extra = null;
            DoCloneBehavior(cloneBehavior, ref treeInstance, ref globalBlackboard, ref extra);
        }

        public void DoCloneBehavior<T>(CloneBehavior cloneBehavior, ref Arbor.TreeInstance treeInstance, ref Arbor.Blackboard globalBlackboard, ref T extra)
        {
            switch (cloneBehavior)
            {
                case CloneBehavior.Nop:
                    break;

                case CloneBehavior.Clone:
                    (treeInstance, globalBlackboard, extra) = Dec.Recorder.Clone((tree: treeInstance, globalBlackboard, extra));
                    break;

                case CloneBehavior.WriteRead:
                    (treeInstance, globalBlackboard, extra) = Dec.Recorder.Read<(Arbor.TreeInstance, Arbor.Blackboard, T)>(Dec.Recorder.Write((tree: treeInstance, globalBlackboard, extra)));
                    break;
            }
        }
    }
}
