using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : SingleTon<EventManager>
{
    


    public void EventNodeStart(EventNode node)
    {
        
    }

    public void Choose(Choice choice)
    {
        Debug.Log($"{choice.baseEvent} : Event ½ÇÇà.");
        choice.baseEvent.Execute();

        NodeManager.Instance.GoToNode(choice.nextNode);
    }
}
