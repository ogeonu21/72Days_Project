using System;
using System.Collections.Generic;
using UnityEngine;

public class StoryManager : SingleTon<StoryManager>
{
    #region 변수그룹
    

    public Node currentNode;

    //Manager instance
    private GameManager gameManager;
    private CombatManager combatManager;
    #endregion

    protected override void Awake()
    {
        base.Awake();

        //[이벤트 구독]
        gameManager = GameManager.Instance;
        combatManager = CombatManager.Instance;

    }


    #region [Progress Manage]
    public void StartNewProgress()
    {
        string startNodeName = "Main_01";
        var node = Resources.Load<Node>($"Nodes/{startNodeName}");
        //Reset PlayerData 함수가 필요. 새로 게임을 시작하면 기존 데이터를 지워야하니까. Json파일을 써야함.

        if (node != null)
        {
            NodeManager.Instance.GoToNode(node);
        }
        else
        {
            Debug.LogError($"노드 '{startNodeName}'를 찾을 수 없습니다.");
        }
    }

    public void LoadProgress(Node node)
    {
        if (node != null)
        {
            NodeManager.Instance.GoToNode(node);
        }
        else
        {
            Debug.LogError($"저장된 노드를 찾을 수 없습니다.");
        }
    }

    #endregion


}
