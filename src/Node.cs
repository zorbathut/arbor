using System.Collections.Generic;

namespace Arbor
{
    public enum Result
    {
        Success,
        Working,
        Failure,
    }

    public class Context
    {
        public List<Node> stack = new List<Node>();
    }

    public abstract class Node
    {
        private IEnumerator<Result> currentWorker;
        private readonly List<Node> children;
        private Tree tree;

        public Node()
        {
            
        }

        protected Node(List<Node> children)
        {
            this.children = children;
        }

        internal void Init(Tree tree)
        {
            this.tree = tree;

            if (children != null)
            {
                foreach (var child in children)
                {
                    child.Init(tree);
                }
            }
        }

        public List<Node> GetChildren()
        {
            return children;
        }

        public Result Update()
        {
            tree.stack.Add(this);

            if (currentWorker == null)
            {
                currentWorker = Worker().GetEnumerator();
            }

            bool moved;
            try
            {
                moved = currentWorker.MoveNext();
            }
            catch (System.Exception e)
            {
                Dbg.Ex(e);
                moved = false;
            }
            tree.stack.RemoveAt(tree.stack.Count - 1);

            if (!moved)
            {
                Dbg.Err("Worker didn't exit properly, assuming failure");
                return Result.Failure;
            }

            var result = currentWorker.Current;
            if (result != Result.Working)
            {
                // we done now
                try
                {
                    Reset();
                }
                catch (System.Exception e)
                {
                    Dbg.Ex(e);
                }
            }

            return result;
        }

        public abstract IEnumerable<Result> Worker();

        public virtual void Reset()
        {
            currentWorker = null;

            // this is currently *very inefficient* and I am doing it anyway
            if (children != null)
            {
                foreach (var child in children)
                {
                    child.Reset();
                }
            }
        }
    }
}
