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
    //다른 Object들도 있어야 함.

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
        CharacterManager.Instance.OnCharacterReady += UpdateCharacter;
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
        switch (node.nodeType)
        {
            case NodeType.MainStoryNode:
                OnStoryUI(node);
                break;
            case NodeType.StoryNode:
                OnStoryUI(node);
                break;
            case NodeType.CombatNode:
                OnCombatUI(node);
                break;
            case NodeType.EventNode:
                break;
            case NodeType.EndingNode:
                break;
            default:
                break;
        }
    }

    private void OnStoryUI(Node node)
    {
        if (!storyUI.gameObject.activeSelf)
        {
            storyUI.gameObject.SetActive(true);
            combatUI.gameObject.SetActive(false);

            enemy.gameObject.SetActive(false);
        }
        else
        {
            storyUI.UpdateStoryUI(node);
        }

    }

    private void OnCombatUI(Node node)
    {
        if (!combatUI.gameObject.activeSelf)
        {
            storyUI.gameObject.SetActive(false);
            combatUI.gameObject.SetActive(true);

            enemy.gameObject.SetActive(true);
            //여기서는 Enemy HP Bar만 활성화 하는 식으로 만들어야해.
        }

        CombatManager.Instance.CombatNodeStart(node);
    }
    #endregion

}
