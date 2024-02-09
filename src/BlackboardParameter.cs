
namespace Arbor
{
    internal struct BlackboardIdentifier
    {
        public string bb;
        public string id;
    }

    public class BlackboardParameter<T>
    {
        BlackboardIdentifier identifier;

        internal BlackboardParameter() { }

        public static BlackboardParameter<T> Tree(string id)
        {
            return new BlackboardParameter<T> { identifier = new BlackboardIdentifier{ bb = "tree", id = id } };
        }
        public static BlackboardParameter<T> Global(string id)
        {
            return new BlackboardParameter<T> { identifier = new BlackboardIdentifier { bb = "global", id = id } };
        }
        public static BlackboardParameter<T> Specific(string bbid, string itemid)
        {
            return new BlackboardParameter<T> { identifier = new BlackboardIdentifier { bb = bbid, id = itemid } };
        }

        public T Get(Tree tree)
        {
            return tree.BlackboardGet<T>(identifier);
        }

        public void Set(Tree tree, T value)
        {
            tree.BlackboardSet<T>(identifier, value);
        }

        public void Register(Tree tree)
        {
            tree.Register<T>(identifier);
        }
    }
}
