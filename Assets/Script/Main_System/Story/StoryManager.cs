using System;
using System.Collections.Generic;
using UnityEngine;

public class StoryManager : SingleTon<StoryManager>
{
    #region 변수그룹
    //노드 변경 감지 이벤트 Action뒤에 <>에는 OnNodeChanged 이벤트를 발생시키면서 매개변수로 전달할 데이터 타입을 는다.
    public event Action<StoryNode> OnStoryNodeChanged;
    public event Action<Choice> OnCombatNodeStart;

    private StoryNode currentNode;


    //Manager instance
    private GameManager gameManager;
    #endregion

    private new void Awake()
    {
        base.Awake();

        //[이벤트 구독]
        gameManager = GameManager.Instance;
        gameManager.OnGameStateChanged += OnApplyUpdateState;

    }

    private void OnApplyUpdateState(GameState state)
    {
        switch (state)
        {
            case GameState.Story:
                
                //GameState가 Story로 진입.
                //전환을 받고 data를 로드 후에 goto해야함.
                break;
            default:
                break;
        }
    }

    #region [Progress Manage]
    public void StartNewProgress(string startNodeName)
    {
        //SaveManager에서 불러와야하나?
        //*** 개선필요!!!!
        var node = Resources.Load<StoryNode>($"Story/{startNodeName}");
        //Reset PlayerData 함수가 필요. 새로 게임을 시작하면 기존 데이터를 지워야하니까. Json파일을 써야함.

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
        //음 아니네 이거는 상관 없겠다. PlayerPrefs로 저장하고 있구나.
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
    #endregion


    public void Choose(int index)
    {
        if (currentNode.choices != null && index < currentNode.choices.Count)
        {
            if (currentNode.choices[index].triggersCombat)
            {
                //전투 시작!
                OnCombatNodeStart?.Invoke(currentNode.choices[index]);
                gameManager.UpdateGameState(GameState.Combat);
                //여기서 문제는 위의 함수가 전부 실행된 이후에 실행이 되냐 아니냐의 문제인데.
                //만약 전투 시작이 끝나고나서 실행된다면 그대로 GoToNode()를 실행.
                //아니라면 Enum을 써야함.
                //일단 전투 시작이 return Win or Defeaut를 통해서 GoToNode()를 제어해야함.

                //여기서 중간에 멈춰!!!
                //GoToNode를 실행해야함!
            }
            else {
                GoToNode(currentNode.choices[index].nextNode);
            }
        }
        else
        {
            Debug.LogWarning("선택지가 올바르지 않음.");
        }
    }

    //Node Update
    public void GoToNode(StoryNode node)
    {
        currentNode = node;
        OnStoryNodeChanged?.Invoke(node);
        SaveProgress(node);
    }

    public StoryNode GetCurrentNode()
    {
        return currentNode;
    }
}
