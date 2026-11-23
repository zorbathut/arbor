
namespace Arbor
{
    [Dec.CloneStructPiecewise]
    internal struct BlackboardIdentifier
    {
        // Zero for a parameter known only by its label, which TreeDec.BlackboardResolve turns into its tree's parameter of that label when the tree is built.
        public ulong uid;

        // Null or empty for a constant, which is no identifier at all.
        public string label;
    }

    internal static class BlackboardStatics
    {
        internal static ulong s_uid = 0;    // always interlocked
    }

    [Dec.CloneStructPiecewise]
    public struct BlackboardParameter<T> : Dec.IRecordable
    {
        internal BlackboardIdentifier identifier;
        internal T constant;

        public static BlackboardParameter<T> Tree(string id)
        {
            // an empty label is how a constant looks, so it can't name a parameter
            if (string.IsNullOrEmpty(id))
            {
                Dbg.Err("Attempted to create a blackboard parameter with an empty label; it would be a constant");
                return default;
            }

            return new BlackboardParameter<T> { identifier = new BlackboardIdentifier{ uid = System.Threading.Interlocked.Increment(ref BlackboardStatics.s_uid), label = id } };
        }

        public static BlackboardParameter<T> Constant(T initial)
        {
            return new BlackboardParameter<T> { constant = initial };
        }

        public T Get()
        {
            if (IsConstant())
            {
                return constant;
            }

            if (!IsResolved())
            {
                Dbg.Err($"Attempted to read blackboard parameter `{this}` before it was resolved; it must belong to a node of a built tree");
                return default;
            }

            return State.Current.Value.BlackboardGet<T>(this);
        }

        public void Set(T value)
        {
            if (IsConstant())
            {
                Dbg.Err("Attempted to set a constant blackboard parameter");
                return;
            }

            if (!IsResolved())
            {
                Dbg.Err($"Attempted to set blackboard parameter `{this}` before it was resolved; it must belong to a node of a built tree");
                return;
            }

            State.Current.Value.BlackboardSet<T>(this, value);
        }

        public bool IsConstant()
        {
            return string.IsNullOrEmpty(identifier.label);
        }

        internal bool IsResolved()
        {
            return identifier.uid != 0;
        }

        // A tree's definition, not its running state: the label that names the parameter, or the constant while there is none. The uid is never recorded, since it comes from a process-wide counter; a parameter read back is known by its label alone until its tree resolves it (TreeDec.BlackboardResolve), so one label names one parameter of each tree that reads it. The key comes first so that its position stays put while the constant comes and goes.
        public void Record(Dec.Recorder recorder)
        {
            // Dec reads into the value already there, which a field initializer may have filled; the record is the whole parameter, so a key it leaves out makes a constant rather than keeping the initializer's.
            if (recorder.Mode == Dec.Recorder.Direction.Read)
            {
                identifier = default;
            }
            recorder.Record(ref identifier.label, "key");
            if (recorder.Mode == Dec.Recorder.Direction.Read || IsConstant())
            {
                recorder.Record(ref constant, nameof(constant));
            }
        }

        public override string ToString()
        {
            if (!IsConstant())
            {
                return $"[{identifier.label}]";
            }
            else
            {
                return $"[Constant:{constant}]";
            }
        }
    }
}
