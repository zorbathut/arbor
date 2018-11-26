
namespace Arbor
{
    public class Select<T> : Node<T>
    {
        private readonly Node<T>[] children;
        private int active = 0;

        public Select(Node<T>[] children, string name = null) : base(name : name)
        {
            this.children = children;
        }

        public override Result UpdateWorker(Context<T> context, T state)
        {
            if (active == children.Length)
            {
                active = 0;
                children[active].Reset();
            }

            while (active < children.Length)
            {
                var result = children[active].Update(context, state);
                if (result == Result.Success)
                {
                    active = children.Length;
                    return Result.Success;
                }
                else if (result == Result.Working)
                {
                    return Result.Working;
                }
                else if (result == Result.Failure)
                {
                    ++active;
                    if (active < children.Length)
                    {
                        children[active].Reset();
                    }

                    continue;
                }
            }

            // I guess we iterated through everything!
            return Result.Failure;
        }

        public override void Reset()
        {
            base.Reset();

            active = children.Length;
        }
    }
}
