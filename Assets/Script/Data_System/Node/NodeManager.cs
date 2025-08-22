using System;
using UnityEngine;

public class NodeManager : SingleTon<NodeManager>
{
    public Node startNode { get; private set; }
    public Node currentNode { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        /// <sumary>
        /// string startNodeName = "Main_01";
        /// var node = Resources.Load<StoryNode>($"Story/{startNodeName}");

    }

    public void GoToNode(Node nextNode)
    {
        //event ¹ß»ý È®·ü? Á¶°Ç Ã¼Å©?
        currentNode = nextNode;
        GameEvent.NotifyNodeChange(nextNode);
        Debug.Log($"³ëµå ¹Ù²î¾ú´ÙÀ×~~ {nextNode.nodeName} : {nextNode.nodeType}");
        GameManager.Instance.SaveGame();
    }
}
