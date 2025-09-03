using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class UIManager : SingleTon<UIManager>
{
    #region [변수 그룹]
    //Object
    [SerializeField]
    private List<UIController> uiControllers = new List<UIController>();
    //다른 Object들도 있어야 함.


    [SerializeField]
    private GameObject levelUpUI;

    //Manager
    private GameManager gameManager;

    //Instance
    private Player player;
    private Enemy enemy;
    #endregion

    #region [initialize]
    protected override void Awake()
    {
        base.Awake();
        InitializeUIControllers();
        GameEvent.OnNodeChanged += UpdateUI;
        GameEvent.OnPlayerLevelUp += UpdateLevelUpUI;

        CharacterManager.Instance.OnCharacterReady += UpdateCharacter;
    }

    void OnDestroy()
    {
        // 오브젝트가 파괴될 때 이벤트 구독을 해지
        GameEvent.OnNodeChanged -= UpdateUI;
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnCharacterReady -= UpdateCharacter;
        }

    }

    private void InitializeUIControllers()
    {
        foreach (var controller in uiControllers)
        {
            if (controller == null)
            {
                Debug.LogError("UI 컨트롤러 목록에 null이 있습니다. Inspector를 확인해주세요!");
            }
        }
    }

    private void UpdateCharacter(Player player, Enemy enemy)
    {
        this.enemy = enemy;
        this.player = player;
    }
    #endregion

    #region [Node UI Control]
    private void UpdateUI(Node node)
    {
        if (node == null)
        {
            Debug.LogWarning("노드 오류 발생");
            return;
        }

        DeactivateAllUI();

        // 노드 타입에 따라 특정 UI 활성화
        switch (node.nodeType)
        {
            case NodeType.MainStoryNode:
            case NodeType.StoryNode:
                ActivateUI<StoryUIController>(node);
                DeactivateEnemy();
                break;
            case NodeType.CombatNode:
                ActivateUI<CombatUIController>(node);
                ActivateEnemy();
                break;
            case NodeType.EventNode:
                ActivateUI<EventUIController>(node);
                DeactivateEnemy();
                break;
            case NodeType.EndingNode:
                ActivateUI<EndingUIController>(node);
                DeactivateEnemy();
                break;
            default:
                Debug.LogWarning($"알 수 없는 노드 타입입니다: {node.nodeType}");
                break;
        }
    }

    private void DeactivateAllUI()
    {
        foreach (var controller in uiControllers)
        {
            if (controller != null)
            {
                controller.gameObject.SetActive(false);
            }
        }
    }

    private void ActivateUI<T>(Node node) where T : MonoBehaviour, IUpdatableUI
    {
        var targetUI = uiControllers.FirstOrDefault(ui => ui is T);
        if (targetUI != null)
        {
            targetUI.gameObject.SetActive(true);
            (targetUI as IUpdatableUI)?.UpdateUI(node);
        }
        else
        {
            Debug.LogError($"{typeof(T).Name} UI를 찾을 수 없습니다. Inspector를 확인하세요.");
        }
    }

    private void ActivateEnemy()
    {
        if (enemy != null)
        {
            enemy.gameObject.SetActive(true);
        }
    }

    private void DeactivateEnemy()
    {
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
        }
    }

    #endregion


    #region [Lv UI Control]
    private void UpdateLevelUpUI()
    {
        Time.timeScale = 0;
        if (levelUpUI != null)
        {
            levelUpUI.SetActive(true);
        }
        
    }

    public void EventExecute(BaseEvent baseEvent)
    {
        Time.timeScale = 1;
        baseEvent.Execute();
        if (levelUpUI != null)
        {
            levelUpUI.SetActive(false);
        }
        
    }
    #endregion
}
