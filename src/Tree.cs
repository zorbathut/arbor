
using System.Collections.Generic;

namespace Arbor
{
    public class Tree
    {
        private readonly Node root;

        internal readonly List<Node> stack = new List<Node>();

        // todo: blackboard?

        public Tree(Node root)
        {
            this.root = root;

            root.Init(this);
        }

        public void Update()
        {
            root.Update();
        }
    }
}
