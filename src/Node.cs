using System.Collections.Generic;

namespace Arbor
{
    public enum Result
    {
        Success,
        Working,
        Failure,
    }

    public class Context<T>
    {
        public List<Node<T>> stack = new List<Node<T>>();
        public T state;
    }

    public abstract class Node<T>
    {
        public Node(string name = null)
        {

        }

        public Result Update(Context<T> context)
        {
            context.stack.Add(this);

            var result = UpdateWorker(context);

            context.stack.RemoveAt(context.stack.Count - 1);

            return result;
        }

        public abstract Result UpdateWorker(Context<T> context);
    }
}
