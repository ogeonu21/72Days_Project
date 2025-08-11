using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : SingleTon<UIManager>
{
    [SerializeField]
    private StoryUIController storyUI;
    [SerializeField]
    private CombatUIController combatUI;


    private GameManager gameManager;

    new void Awake()
    {
        base.Awake();
        gameManager = GameManager.Instance;
        gameManager.OnGameStateChanged += UpdateUI;
    }


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
        if (!storyUI.gameObject.activeSelf)
        {
            storyUI.gameObject.SetActive(true);
            combatUI.gameObject.SetActive(false);
        }
            
    }

    private void OnCombatUI()
    {
        if (!combatUI.gameObject.activeSelf)
        {
            storyUI.gameObject.SetActive(false);
            combatUI.gameObject.SetActive(true);

            //여기서는 Enemy HP Bar만 활성화 하는 식으로 만들어야해.
        }
            
    }
    public void TestSTate()
    {
        gameManager.UpdateGameState(GameState.Story);
    }
}
