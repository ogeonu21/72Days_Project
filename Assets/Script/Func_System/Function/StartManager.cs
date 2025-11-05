using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartManager : SingleTon<StartManager>
{
    private GameManager gameManager;

    protected override void Awake()
    {
        base.Awake();

        gameManager = GameManager.Instance;
    }

    public void OnNewGameStart()
    {
        gameManager.StartNewGame();
    }

    public void OnLoadGame()
    {
        gameManager.LoadGame();
    }
}
