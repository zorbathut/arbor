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
    }

    public abstract class Node<T>
    {
        public Node(string name = null)
        {

        }

        public Result Update(Context<T> context, T state)
        {
            context.stack.Add(this);

            var result = UpdateWorker(context, state);

            context.stack.RemoveAt(context.stack.Count - 1);

            return result;
        }

        public abstract Result UpdateWorker(Context<T> context, T state);

        public virtual void Reset() { }
    }
}
