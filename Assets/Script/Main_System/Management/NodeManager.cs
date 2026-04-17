using System;
using UnityEngine;

public class NodeManager : SingleTon<NodeManager>
{
    public Node startNode { get; private set; }
    public Node currentNode { get; private set; }
    public MainStoryNode dumpNode;

    protected override void Awake()
    {
        base.Awake();
        /// <sumary>
        /// string startNodeName = "Main_01";
        /// var node = Resources.Load<StoryNode>($"Story/{startNodeName}");

    }

    public void GoToNode(Node nextNode)
    {
        //event 발생 확률? 조건 체크?
        currentNode = nextNode;
        Debug.Log($"<color=beige>[NodeManager] </color> 노드를 이동합니다. 현재 노드 : {currentNode.nodeName}");
        GameEvent.NotifyNodeChange(currentNode);
        GameEvent.SaveGame();
    }
}
