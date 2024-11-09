
namespace Arbor
{
    [Dec.CloneStructPiecewise]
    internal struct BlackboardIdentifier : Dec.IRecordable
    {
        public string id;

        public void Record(Dec.Recorder recorder)
        {
            recorder.Record(ref id, nameof(id));
        }
    }

    [Dec.CloneStructPiecewise]
    public struct BlackboardParameter<T> : Dec.IRecordable
    {
        BlackboardIdentifier? identifier;
        T constant;

        public static BlackboardParameter<T> Tree(string id)
        {
            return new BlackboardParameter<T> { identifier = new BlackboardIdentifier{ id = id } };
        }

        public static BlackboardParameter<T> Constant(T initial)
        {
            return new BlackboardParameter<T> { constant = initial };
        }

        public T Get()
        {
            if (identifier.HasValue)
            {
                return State.Current.Value.BlackboardGet<T>(identifier.Value);
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
                State.Current.Value.BlackboardSet<T>(identifier.Value, value);
            }
            else
            {
                Dbg.Err("Attempted to set a constant blackboard parameter");
            }
        }

        public void RegisterWith(Blackboard blackboard)
        {
            if (identifier.HasValue)
            {
                blackboard.Register(identifier.Value.id, typeof(T));
            }
        }

        public void Record(Dec.Recorder recorder)
        {
            recorder.RecordAsThis(ref identifier);
            recorder.RecordAsThis(ref constant);
        }
    }
}
