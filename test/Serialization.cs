
using Arbor;
using NUnit.Framework;
using System.Collections.Generic;

namespace ArborTest
{
    [TestFixture]
    public partial class Serialization : Base
    {
        public partial class DoNothingWith : Arbor.Node
        {
            public BlackboardParameter<string> DataId;

            public override IEnumerable<Result> Worker()
            {
                // Just a placeholder to do nothing
                yield return Result.Success;
            }
        }

        public class ParameterA : Arbor.TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> data = BlackboardParameter<string>.Tree("data");

            public Node Create(TreeDec tree)
            {
                return new DoNothingWith() { DataId = data };
            }
        }

        public class ParameterB : Arbor.TreeDec.ITreeFactory
        {
            public static BlackboardParameter<string> data = BlackboardParameter<string>.Tree("data");

            public Node Create(TreeDec tree)
            {
                return new DoNothingWith() { DataId = data };
            }
        }





    }
}