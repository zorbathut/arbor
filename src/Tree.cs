
using System.Collections.Generic;

namespace Arbor
{
    public class Tree
    {
        public static System.Threading.ThreadLocal<Tree> Current = new();
        private struct Scope : System.IDisposable
        {
            private Tree old;
            private Tree current;
            public Scope(Tree tree, Blackboard global)
            {
                old = Current.Value;
                current = tree;
                Current.Value = current;
                current.blackboards["global"] = global;
            }

            public void Dispose()
            {
                Assert.AreSame(Current.Value, current);
                current.blackboards["global"] = null;
                Current.Value = old;
            }
        }

        private readonly Node root;

        internal readonly List<Node> stack = new List<Node>();

        private readonly Dictionary<string, Blackboard> blackboards = new Dictionary<string, Blackboard>();

        public Tree(Node root, Blackboard global)
        {
            this.root = root;

            blackboards["tree"] = new Blackboard();

            using (new Scope(this, global))
            {
                root?.Init();
            }
        }

        public void Update(Blackboard global)
        {
            using (new Scope(this, global))
            {
                root?.Update();
            }
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
