
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor
{
    public class TreeDec : Dec.Dec
    {
        public object worker;

        public interface ITreeFactory
        {
            Node Create(Blackboard blackboardDescriptor);
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
                reporter("Worker is null; at the moment this is mandatory");
                return;
            }

            if (!(worker is ITreeFactory))
            {
                reporter("Worker is not ITreeFactory; at the moment this is mandatory");
            }
        }

        public override void PostLoad(Action<string> reporter)
        {
            base.PostLoad(reporter);

            blackboardDescriptor = new Blackboard();

            Blackboard.writeOnly = true;
            root = (worker as ITreeFactory).Create(blackboardDescriptor);

            // right now this really does not support parallelism but that's OK
            Node.initRunning = true;
            var nodeList = new List<Node>();
            root?.Init(blackboardDescriptor, nodeList);
            Node.initRunning = false;

            Blackboard.writeOnly = false;

            // for the sake of a little extra efficiency
            nodes = nodeList.ToArray();

            if (nodes.Distinct().Count() != nodes.Length)
            {
                reporter("Nodes used multiple times in tree, each node must have exactly one parent");
            }

            // do this manually (this is ugly!)
            var arrayPath = new Dec.PathMember(new Dec.PathDec(typeof(TreeDec), DecName), "nodeList");
            for (int i = 0; i < nodes.Length; ++i)
            {
                Dec.Database.RegisterLookup(nodes[i], new Dec.PathIndex(arrayPath, i));
            }
        }
    }
}
