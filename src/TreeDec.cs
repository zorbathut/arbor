using System;

namespace Arbor;

using System.Collections.Generic;

public class TreeDec : Dec.Dec
{
    // node 0 is our root
    [NonSerialized] internal List<Node> nodes = new List<Node>();

    // exactly one of these must be filled out
    public Node root;
    public Type factory;

    public override void PostLoad(Action<string> reporter)
    {
        base.PostLoad(reporter);

        if (factory != null)
        {
            if (root != null)
            {
                reporter("`root` and `factory` both provided; this is probably a mistake");
            }
            else
            {
                var factoryInstance = (TreeFactory)Activator.CreateInstance(factory);
                root = factoryInstance.Create();
            }
        }

        root.UnrollTo(this);
    }

    public int RegisterUnrollNode(Node node)
    {
        int index = nodes.Count;

        nodes.Add(node);

        return index;
    }
}
