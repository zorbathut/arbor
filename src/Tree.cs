
namespace Arbor
{
    public class Tree<T>
    {
        private Node<T> root;

        public Tree(Node<T> root)
        {
            this.root = root;
        }

        public void Update(T state)
        {
            var context = new Context<T>() { state = state };

            root.Update(context);
        }
    }
}
