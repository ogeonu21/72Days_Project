using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
[CreateAssetMenu(fileName = "StoryNode", menuName = "Node/StoryNode", order = 1)]
public class StoryNode : Node
{
    public List<Choice> choices = new List<Choice>();
}

