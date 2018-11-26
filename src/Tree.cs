
namespace Arbor
{
    public class Tree<T>
    {
        private readonly Node<T> root;
        private readonly Context<T> context;

        public T State { get; }

        public Tree(Node<T> root, T state)
        {
            this.root = root;
            this.State = state;
            this.context = new Context<T>();
        }

        public void Update(T state)
        {
            root.Update(context, state);
        }
    }
}
