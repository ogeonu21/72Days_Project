using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
[CreateAssetMenu(fileName = "EventNode", menuName = "Node/EventNode", order = 3)]
public class EventNode : Node
{
    public string eventName;
    public List<Choice> choices = new List<Choice>();
}
