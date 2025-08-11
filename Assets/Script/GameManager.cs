using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : SingleTon<GameManager>
{
    #region [변수 관리]

    public int survive_data; // 생존 날짜

    //진행도 관리
    public string currentNodeName; // 진행 저장용
    private const string SaveKey = "CurrentNode";

    private GameState currentState;
    #endregion

    #region [initialization]

    // 게임 로직 초기화
    private void Start()
    {
    }
    #endregion

    #region [이벤트 관리]
    //GameStateChanged를 Notify할 Event
    public event Action<GameState> OnGameStateChanged;

    #endregion


    #region 진행 관리

    //새로운 게임 시작
    public void StartNewGame(string startNodeName)
    {
        currentNodeName = startNodeName;
        SceneManager.LoadScene("GameWindow");
        //SaveManger에서 불러오고 시작.
        StoryManager.Instance.StartNewProgress(currentNodeName);
    }

    //기존 게임 불러오기
    public void LoadGame()
    {
        currentNodeName = PlayerPrefs.GetString(SaveKey, "StartNode");
        SceneManager.LoadScene("GameWindow");

        //SaveManger에서 불러오고 시작.
        StoryManager.Instance.LoadProgress();
    }

    //진행도 저장
    //SaveManger로 옮겨야해.
    public void SaveProgress(string nodeName)
    {
        currentNodeName = nodeName;
        PlayerPrefs.SetString(SaveKey, nodeName);
        PlayerPrefs.Save();
    }
    #endregion

    #region [GameState 관리]

    //게임 State 관리
    public void UpdateGameState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
    #endregion

    #region [이벤트 관리]
   


    #endregion
}
