using UnityEngine;

public class BaseEvent : ScriptableObject, IEvent
{
    public virtual void Execute() {}

    public virtual void Execute(Node nextNode) {}
}
