using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : SingleTon<EventManager>
{
    public void Choose(Choice choice)
    {
        Debug.Log($"{choice.baseEvent} : Event 실행.");
        choice.baseEvent.Execute(choice.nextNode);
    }
}
