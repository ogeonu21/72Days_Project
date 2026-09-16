using UnityEngine;

/// <summary>
/// 노드 진행 상태와 스토리 선택의 유효성을 소유한다.
/// UI는 이 클래스의 상태를 변경하지 않고 NodeManager를 통해 선택만 제출한다.
/// </summary>
public class NodeRunner
{
    public Node CurrentNode { get; private set; }

    public bool TryEnter(Node nextNode)
    {
        if (nextNode == null)
        {
            Debug.LogWarning("[NodeRunner] 다음 노드가 없어 진행할 수 없습니다.");
            return false;
        }

        CurrentNode = nextNode;
        return true;
    }

    public bool TryAdvanceMainStory(MainStoryNode sourceNode)
    {
        if (sourceNode == null || sourceNode != CurrentNode)
        {
            Debug.LogWarning("[NodeRunner] 현재 메인 스토리 노드가 아닌 요청은 무시했습니다.");
            return false;
        }

        return TryEnter(sourceNode.nextNode);
    }

    public bool TrySelectStoryChoice(Choice choice)
    {
        StoryNode storyNode = CurrentNode as StoryNode;
        if (storyNode == null || choice == null || storyNode.choices == null || !storyNode.choices.Contains(choice))
        {
            Debug.LogWarning("[NodeRunner] 현재 스토리 노드에 없는 선택입니다.");
            return false;
        }

        return TryEnter(choice.nextNode);
    }
}
