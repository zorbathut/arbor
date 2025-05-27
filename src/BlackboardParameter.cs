
namespace Arbor
{
    [Dec.CloneStructPiecewise]
    internal struct BlackboardIdentifier : Dec.IRecordable
    {
        public ulong uid;
        public string label;

        public void Record(Dec.Recorder recorder)
        {
            recorder.Record(ref uid, nameof(uid));
            recorder.Record(ref label, nameof(label));
        }
    }

    [Dec.CloneStructPiecewise]
    public struct BlackboardParameter<T> : Dec.IRecordable
    {
        internal BlackboardIdentifier? identifier;
        internal T constant;

        private static ulong s_uid = 0;    // always interlocked

        public static BlackboardParameter<T> Tree(string id)
        {
            return new BlackboardParameter<T> { identifier = new BlackboardIdentifier{ uid = System.Threading.Interlocked.Increment(ref s_uid), label = id } };
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

        public void Record(Dec.Recorder recorder)
        {
            recorder.RecordAsThis(ref identifier);
            recorder.RecordAsThis(ref constant);
        }
    }
}
