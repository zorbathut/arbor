
using System.Collections.Generic;

namespace Arbor
{
    public class Tree
    {
        private readonly Node root;

        private readonly List<Node> stack = new List<Node>();

        internal readonly Context context;

        // todo: blackboard?

        public Tree(Node root)
        {
            this.root = root;

            this.context = new Context();

            root.Init(this);
        }

        public void Update()
        {
            root.Update();
        }
    }
}
