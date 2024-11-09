
using System;
using System.Collections.Generic;

namespace Arbor
{
    public class TreeDec : Dec.Dec
    {
        public object worker;

        public interface ITreeFactory
        {
            Node Create();
        }

        [NonSerialized]
        internal Node root;

        [NonSerialized]
        internal Node[] nodes;

        [NonSerialized]
        internal Blackboard blackboardDescriptor;

        public override void ConfigErrors(Action<string> reporter)
        {
            base.ConfigErrors(reporter);

            if (worker == null)
            {
                reporter("Worker is null");
                return;
            }

            if (!(worker is ITreeFactory))
            {
                reporter("Worker is not ITreeFactory; at the moment this is mandatory");
            }

            root = (worker as ITreeFactory).Create();
        }

        public override void PostLoad(Action<string> reporter)
        {
            base.PostLoad(reporter);

            blackboardDescriptor = new Blackboard();

            var nodeList = new List<Node>();
            root?.Init(blackboardDescriptor, nodeList);

            // for the sake of a little extra efficiency
            nodes = nodeList.ToArray();

            // do this manually (this is ugly!)
            var arrayPath = new Dec.PathMember(new Dec.PathDec(typeof(TreeDec), DecName), "nodeList");
            for (int i = 0; i < nodes.Length; ++i)
            {
                Dec.Database.RegisterLookup(nodes[i], new Dec.PathIndex(arrayPath, i));
            }
        }
    }
}
