
namespace Arbor
{
    [Dec.CloneStructPiecewise]
    internal struct BlackboardIdentifier : Dec.IRecordable
    {
        public ulong uid;
        public string label;

        public void Record(Dec.Recorder recorder)
        {
            if (recorder.Mode == Dec.Recorder.Direction.Read)
            {
                // Get the ID
                int id = 0;
                recorder.RecordAsThis(ref id);

                // Remap this according to our state's tree's remap
                var assign = State.Current.Value.tree.blackboardLocalId[id];
                uid = assign.uid;
                label = assign.label;
            }
            else
            {
                // Store the lookup ID
                int id = State.Current.Value.tree.blackboardLocalIdLookup[uid];
                recorder.RecordAsThis(ref id);
            }
        }
    }

    internal static class BlackboardStatics
    {
        internal static ulong s_uid = 0;    // always interlocked
    }

    [Dec.CloneStructPiecewise]
    public struct BlackboardParameter<T> : Dec.IRecordable
    {
        internal BlackboardIdentifier? identifier;
        internal T constant;

        public static BlackboardParameter<T> Tree(string id)
        {
            return new BlackboardParameter<T> { identifier = new BlackboardIdentifier{ uid = System.Threading.Interlocked.Increment(ref BlackboardStatics.s_uid), label = id } };
        }

        public static BlackboardParameter<T> Constant(T initial)
        {
            return new BlackboardParameter<T> { constant = initial };
        }

        public T Get()
        {
            if (identifier.HasValue)
            {
                return State.Current.Value.BlackboardGet<T>(this);
            }
            else
            {
                return constant;
            }
        }

        public void Set(T value)
        {
            if (identifier.HasValue)
            {
                State.Current.Value.BlackboardSet<T>(this, value);
            }
            else
            {
                Dbg.Err("Attempted to set a constant blackboard parameter");
            }
        }

        public bool IsConstant()
        {
            return !identifier.HasValue;
        }

        public void Record(Dec.Recorder recorder)
        {
            recorder.RecordAsThis(ref identifier);
            recorder.RecordAsThis(ref constant);
        }

        public override string ToString()
        {
            if (identifier.HasValue)
            {
                return $"[{identifier.Value.label}]";
            }
            else
            {
                return $"[Constant:{constant}]";
            }
        }
    }
}
