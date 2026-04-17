using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
[CreateAssetMenu(fileName = "MainStoryNode", menuName = "Node/MainStoryNode", order = 0)]
public class MainStoryNode : Node
{
    public Node nextNode;
}
