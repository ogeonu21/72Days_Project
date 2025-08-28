using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : SingleTon<UIManager>
{
    #region [변수 그룹]
    //Object
    [SerializeField]
    private StoryUIController storyUI;
    [SerializeField]
    private CombatUIController combatUI;
    [SerializeField]
    private EventUIController eventUI;
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
    new void Awake()
    {
        base.Awake();
        GameEvent.OnNodeChanged += UpdateUI;
        GameEvent.OnPlayerLevelUp += UpdateLevelUpUI;

        CharacterManager.Instance.OnCharacterReady += UpdateCharacter;

        
    }

    void OnDestroy()
    {
        // 오브젝트가 파괴될 때 이벤트 구독을 해지
        GameEvent.OnNodeChanged -= UpdateUI;
        CharacterManager.Instance.OnCharacterReady -= UpdateCharacter;
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
        Debug.Log(node.nodeType + ": UpdateUI에서 해당 노드 호출을 시도");
        if (node == null)
        {
            Debug.LogWarning("노드 오류 발생");
            return;
        }

        if (eventUI == null)
        {
            Debug.LogWarning("Error 발생, eventUI를 찾을 수 없음.");
        }

        switch (node.nodeType)
        {
            case NodeType.MainStoryNode: 
            case NodeType.StoryNode:
                OnStoryUI(node);
                break;
            case NodeType.CombatNode:
                OnCombatUI(node);
                break;
            case NodeType.EventNode:
                OnEventUI(node);
                break;
            case NodeType.EndingNode:
                break;
            default:
                break;
        }
    }

    private void OnStoryUI(Node node)
    {
        if (storyUI == null || combatUI == null || eventUI == null) return;
        storyUI.gameObject.SetActive(true);
        combatUI.gameObject.SetActive(false);
        eventUI.gameObject.SetActive(false);

        // enemy 오브젝트도 null 체크 후 비활성화
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
        }

        // 스토리 출력
        storyUI.UpdateStoryUI(node);

    }

    private void OnCombatUI(Node node)
    {
        if (storyUI == null || combatUI == null || eventUI == null) return;
        // null 체크는 유지

        storyUI.gameObject.SetActive(false);
        combatUI.gameObject.SetActive(true);
        eventUI.gameObject.SetActive(false);

        // enemy 오브젝트도 null 체크 후 활성화
        if (enemy != null)
        {
            enemy.gameObject.SetActive(true);
        }

        // 전투 시작
        CombatManager.Instance.CombatNodeStart(node);
    }

    private void OnEventUI(Node node) {
        if (storyUI == null || combatUI == null || eventUI == null) return;
        
        storyUI.gameObject.SetActive(false);
        combatUI.gameObject.SetActive(false);
        eventUI.gameObject.SetActive(true);
        
        // enemy 오브젝트도 null 체크 후 비활성화
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
        }
        // 스토리 출력
        eventUI.UpdateEventUI(node);
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
