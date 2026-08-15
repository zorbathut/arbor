using NUnit.Framework;
using System;
using System.Reflection;

namespace ArborTest
{
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
        private Func<string, bool> errorValidator = null;

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

                // we forgot to do the string interpolation correctly
                Assert.IsFalse(str.Contains("{"));
                Assert.IsFalse(str.Contains("}"));

                // Check to see if this is considered a "valid" error.
                Assert.IsTrue(errorValidator == null || errorValidator(str), $"Error message validation failed: {str}");

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

            Dec.RecorderEnumerator.Config.Setup();
        }

        // Dec.Config.UnitTestParameters is internal to Dec, so we mirror it here and copy it across by reflection.
        public class UnitTestParameters
        {
            public Type[] explicitTypes = null;
            public Type[] explicitStaticRefs = null;
            public Type[] explicitConverters = null;
            public Type[] explicitSetupScanTypes = null;
        }

        public static void UpdateTestParameters(UnitTestParameters parameters)
        {
            var field = typeof(Dec.Config).GetField("TestParameters", BindingFlags.NonPublic | BindingFlags.Static);

            object decParameters = null;
            if (parameters != null)
            {
                decParameters = Activator.CreateInstance(field.FieldType, true);
                foreach (var member in typeof(UnitTestParameters).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    field.FieldType.GetField(member.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(decParameters, member.GetValue(parameters));
                }
            }

            field.SetValue(null, decParameters);
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

        protected void ExpectErrors(Action action, Func<string, bool> errorValidator = null)
        {
            Assert.IsFalse(handlingErrors);
            handlingErrors = true;
            handledError = false;
            this.errorValidator = errorValidator;

            action();

            Assert.IsTrue(handlingErrors);
            Assert.IsTrue(handledError);
            handlingErrors = false;
            handledError = false;
            this.errorValidator = null;
        }

        public enum CloneBehavior
        {
            Nop,
            Clone,
            WriteRead,
        }

        public void DoCloneBehavior(CloneBehavior cloneBehavior, ref Arbor.State state)
        {
            object extra = null;
            DoCloneBehavior(cloneBehavior, ref state, ref extra);
        }

        public void DoCloneBehavior<T>(CloneBehavior cloneBehavior, ref Arbor.State state, ref T extra)
        {
            var origState = state;

            switch (cloneBehavior)
            {
                case CloneBehavior.Nop:
                    break;

                case CloneBehavior.Clone:
                    (state, extra) = Dec.Recorder.Clone((state, extra));
                    break;

                case CloneBehavior.WriteRead:
                    (state, extra) = Dec.Recorder.Read<(Arbor.State, T)>(Dec.Recorder.Write((state, extra)));
                    break;
            }

            Dec.Recorder.ChecksumDiff(origState, state, Assert.Fail);
        }
    }

    public partial class Fail : Arbor.Node
    {
        public override System.Collections.Generic.IEnumerable<Arbor.Result> Worker()
        {
            yield return Arbor.Result.Failure;
        }
    }

    public partial class Succeed : Arbor.Node
    {
        public override System.Collections.Generic.IEnumerable<Arbor.Result> Worker()
        {
            yield return Arbor.Result.Success;
        }
    }
}
