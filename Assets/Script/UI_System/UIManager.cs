using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : SingleTon<UIManager>
{
    #region [변수 그룹]
    [SerializeField]
    private GameObject storyUI;
    [SerializeField]
    private GameObject combatUI;

    private GameManager gameManager;

    private Player player; 
    private Enemy enemy;


    #endregion

    #region [initialize]
    new void Awake()
    {
        base.Awake();
        gameManager = GameManager.Instance;
        CharacterManager.Instance.OnCharacterReady += UpdateCharacter;

    }

    private void OnEnable()
    {
        GameManager.Instance.OnGameStateChanged += UpdateUI;
    }

    private void UpdateCharacter(Player player, Enemy enemy)
    {
        this.enemy = enemy;
        this.player = player;
    }

    #endregion

    private void UpdateUI(GameState state)
    {
        switch (state)
        {
            case GameState.Story:
                OnStoryUI();
                break;
            case GameState.Combat:
                OnCombatUI();
                break;
            default:
                break;
        }
    }

    private void OnStoryUI()
    {
        if (!storyUI.activeSelf)
        {
            storyUI.SetActive(true);
            combatUI.SetActive(false);

            enemy.gameObject.SetActive(false);
        }
            
    }

    private void OnCombatUI()
    {
        if (!combatUI.activeSelf)
        {
            storyUI.SetActive(false);
            combatUI.SetActive(true);

            enemy.gameObject.SetActive(true);
            //여기서는 Enemy HP Bar만 활성화 하는 식으로 만들어야해.
        }
            
    }
    public void TestSTate()
    {
        gameManager.UpdateGameState(GameState.Story);
    }

}
