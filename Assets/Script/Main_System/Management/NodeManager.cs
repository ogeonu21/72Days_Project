using System;
using UnityEngine;

public class NodeManager : SingleTon<NodeManager>
{
    public Node startNode { get; private set; }
    public Node currentNode => nodeRunner != null ? nodeRunner.CurrentNode : null;
    public MainStoryNode dumpNode;
    private NodeRunner nodeRunner;

    protected override void Awake()
    {
        base.Awake();
        nodeRunner = new NodeRunner();
        /// <sumary>
        /// string startNodeName = "Main_01";
        /// var node = Resources.Load<StoryNode>($"Story/{startNodeName}");

    }

    public void GoToNode(Node nextNode)
    {
        if (!nodeRunner.TryEnter(nextNode))
        {
            return;
        }

        PublishNodeChanged();
    }

    public void AdvanceMainStory(MainStoryNode node)
    {
        if (!nodeRunner.TryAdvanceMainStory(node))
        {
            return;
        }

        PublishNodeChanged();
    }

    public void SelectStoryChoice(Choice choice)
    {
        if (!nodeRunner.TrySelectStoryChoice(choice))
        {
            return;
        }

        PublishNodeChanged();
    }

    private void PublishNodeChanged()
    {
        Debug.Log($"<color=beige>[NodeManager] </color> 노드를 이동합니다. 현재 노드 : {currentNode.nodeName}");
        GameEvent.NotifyNodeChange(currentNode);
        GameEvent.SaveGame();
    }
}
