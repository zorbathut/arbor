
namespace Arbor
{
    internal struct BlackboardIdentifier : Dec.IRecordable
    {
        public string id;

        public void Record(Dec.Recorder recorder)
        {
            recorder.Record(ref id, nameof(id));
        }
    }

    public struct BlackboardParameter<T> : Dec.IRecordable
    {
        BlackboardIdentifier identifier;

        public static BlackboardParameter<T> Tree(string id)
        {
            return new BlackboardParameter<T> { identifier = new BlackboardIdentifier{ id = id } };
        }

        public T Get()
        {
            return State.Current.Value.BlackboardGet<T>(identifier);
        }

        public void Set(T value)
        {
            State.Current.Value.BlackboardSet<T>(identifier, value);
        }

        public void RegisterWith(Blackboard blackboard)
        {
            blackboard.Register(identifier.id, typeof(T));
        }

        public void Record(Dec.Recorder recorder)
        {
            recorder.RecordAsThis(ref identifier);
        }
    }
}
