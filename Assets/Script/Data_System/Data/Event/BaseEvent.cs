using UnityEngine;

public class BaseEvent : ScriptableObject, IEvent
{
    public string eventCategory;
    public virtual void Execute() {}

    public virtual void Execute(Node nextNode) {}
}
