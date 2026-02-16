namespace Arbor;

public static class NameAttachments
{
    public static T Name<T>(this T node, string name) where T : Arbor.Node
    {
        node.Name = name;
        return node;
    }
}
