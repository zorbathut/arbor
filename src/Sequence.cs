
namespace Arbor
{
    public class Sequence<T> : Node<T>
    {
        private readonly Node<T>[] children;
        private int active = 0;

        public Sequence(Node<T>[] children, string name = null) : base(name : name)
        {
            this.children = children;
        }

        public override Result UpdateWorker(Context<T> context)
        {
            if (active == children.Length)
            {
                active = 0;
            }

            while (active < children.Length)
            {
                var result = children[active].Update(context);
                if (result == Result.Success)
                {
                    ++active;
                    continue;
                }
                else if (result == Result.Working)
                {
                    return Result.Working;
                }
                else if (result == Result.Failure)
                {
                    active = 0;
                    return Result.Failure;
                }
            }

            // I guess we iterated through everything!
            return Result.Success;
        }
    }
}
