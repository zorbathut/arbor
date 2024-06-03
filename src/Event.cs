namespace Arbor;

using System;

public class BaseEventDec : Dec.Dec
{

}

public class EventDec : BaseEventDec { }
public class EventDec<T1> : BaseEventDec { }
public class EventDec<T1, T2> : BaseEventDec { }
public class EventDec<T1, T2, T3> : BaseEventDec { }
public class EventDec<T1, T2, T3, T4> : BaseEventDec { }

public static class EventAttachments
{
    public static T EventAttach<T>(this T node, EventDec ev, Action action) where T : Arbor.Node
    {
        node.EventAttach_Internal(ev, action);
        return node;
    }

    public static T EventAttach<T, U1>(this T node, EventDec<U1> ev, Action<U1> action) where T : Arbor.Node
    {
        node.EventAttach_Internal(ev, action);
        return node;
    }

    public static T EventAttach<T, U1, U2>(this T node, EventDec<U1, U2> ev, Action<U1, U2> action) where T : Arbor.Node
    {
        node.EventAttach_Internal(ev, action);
        return node;
    }

    public static T EventAttach<T, U1, U2, U3>(this T node, EventDec<U1, U2, U3> ev, Action<U1, U2, U3> action) where T : Arbor.Node
    {
        node.EventAttach_Internal(ev, action);
        return node;
    }

    public static T EventAttach<T, U1, U2, U3, U4>(this T node, EventDec<U1, U2, U3, U4> ev, Action<U1, U2, U3, U4> action) where T : Arbor.Node
    {
        node.EventAttach_Internal(ev, action);
        return node;
    }
}
