namespace Arbor;

public class BasePropertyDec : Dec.Dec
{

}

public class PropertyDec<T1> : BasePropertyDec { }

public static class PropertyAttachments
{
    public static T PropertyAttach<T, U>(this T node, PropertyDec<U> ev, U data) where T : Arbor.Node
    {
        node.PropertyAttach_Internal(ev, data);
        return node;
    }
}
