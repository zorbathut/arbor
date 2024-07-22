namespace Arbor
{
    public static class TreeExecution
    {
        public static Result Update(Node node)
        {
            var tree = Tree.Current.Value;
            tree.stack.Add(node);

            // get it in the tree in the right order
            int activeIndex = tree.active.Count;
            tree.active.Add(node);

            int treeIndex = tree.nodes.IndexOf(node);

            bool moved;
            try
            {
                if (tree.workers[treeIndex] == null)
                {
                    tree.workers[treeIndex] = node.Worker().GetEnumerator();
                }

                moved = tree.workers[treeIndex].MoveNext();
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
                try
                {
                    Terminate(treeIndex);
                }
                catch (System.Exception e)
                {
                    Dbg.Ex(e);
                }

                tree.active[activeIndex] = null; // nope, not active anymore
                return Result.Failure;
            }

            var result = tree.workers[treeIndex].Current;
            if (result != Result.Working)
            {
                // we done now
                try
                {
                    Terminate(treeIndex);
                }
                catch (System.Exception e)
                {
                    Dbg.Ex(e);
                }
                tree.active[activeIndex] = null; // nope, not active anymore
            }

            return result;
        }

        // this is the wrong place for this
        public static void Terminate(Node node)
        {
            Terminate(Tree.Current.Value.nodes.IndexOf(node));
        }

        internal static void Terminate(int index)
        {
            var tree = Tree.Current.Value;

            if (tree.workers[index] == null)
            {
                return;
            }

            tree.workers[index].Dispose();
            tree.workers[index] = null;

            foreach (var childIndex in tree.nodes[index].childLinks)
            {
                Terminate(childIndex);
            }
        }
    }
}