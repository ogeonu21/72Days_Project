using System;
using System.Collections.Generic;
using UnityEngine;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    public event Action<StoryNode> OnNodeChanged;

    private StoryNode currentNode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartNewGame(string startNodeName = "Start")
    {
        var node = Resources.Load<StoryNode>($"Story/{startNodeName}");
        if (node != null)
        {
            GoToNode(node);
        }
        else
        {
            Debug.LogError($"노드 '{startNodeName}'를 찾을 수 없습니다.");
        }
    }

    public void LoadProgress()
    {
        string nodeName = PlayerPrefs.GetString("CurrentStoryNode", "Start");
        var node = Resources.Load<StoryNode>($"Story/{nodeName}");
        if (node != null)
        {
            GoToNode(node);
        }
        else
        {
            Debug.LogError($"저장된 노드 '{nodeName}'를 찾을 수 없습니다.");
        }
    }

    public void SaveProgress(StoryNode node)
    {
        PlayerPrefs.SetString("CurrentStoryNode", node.name);
        PlayerPrefs.Save();
    }

    public void Choose(int index)
    {
        if (currentNode.choices != null && index < currentNode.choices.Count)
        {
            GoToNode(currentNode.choices[index].nextNode);
        }
        else
        {
            Debug.LogWarning("선택지가 올바르지 않음.");
        }
    }

    public void GoToNode(StoryNode node)
    {
        currentNode = node;
        OnNodeChanged?.Invoke(node);
        SaveProgress(node);
    }

    public StoryNode GetCurrentNode()
    {
        return currentNode;
    }
}
