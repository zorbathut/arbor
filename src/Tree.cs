
using System.Collections.Generic;

namespace Arbor
{
    public class Tree
    {
        private readonly Node root;

        internal readonly List<Node> stack = new List<Node>();

        private readonly Dictionary<string, Blackboard> blackboards = new Dictionary<string, Blackboard>();

        public Tree(Node root)
        {
            this.root = root;

            blackboards["tree"] = new Blackboard();
            blackboards["global"] = Arbor.Blackboard.Global;

            root?.Init(this);
        }

        public void Update()
        {
            root?.Update();
        }

        public Blackboard Blackboard(string id)
        {
            return blackboards[id];
        }

        internal T BlackboardGet<T>(BlackboardIdentifier identifier)
        {
            return blackboards[identifier.bb].Get<T>(identifier.id);
        }

        internal void BlackboardSet<T>(BlackboardIdentifier identifier, T item)
        {
            blackboards[identifier.bb].Set<T>(identifier.id, item);
        }

        internal void Register<T>(BlackboardIdentifier identifier)
        {
            blackboards[identifier.bb].Register(identifier.id, typeof(T));
        }
    }
}
