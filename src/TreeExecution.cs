namespace Arbor
{
    public static class TreeExecution
    {
        public static Result Update(Node node)
        {
            var tree = TreeInstance.Current.Value;

            // get it in the tree in the right order
            int activeIndex = tree.active.Count;
            tree.active.Add(node);

            int treeIndex = tree.treeDec.nodes.IndexOf(node);

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

        public static void Terminate(Node node)
        {
            Terminate(TreeInstance.Current.Value.treeDec.nodes.IndexOf(node));
        }

        internal static void Terminate(int index)
        {
            var tree = TreeInstance.Current.Value;

            if (tree.workers[index] == null)
            {
                return;
            }

            tree.workers[index].Dispose();
            tree.workers[index] = null;

            foreach (var childIndex in tree.treeDec.nodes[index].childLinks)
            {
                Terminate(childIndex);
            }
        }
    }
}