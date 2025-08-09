using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : SingleTon<GameManager>
{
    #region [변수 관리]

    // 게임 내의 다른 매니저들을 관리
    public Player player;
    public Enemy enemy;

    public int survive_data; // 생존 날짜

    //진행도 관리
    public string currentNodeName; // 진행 저장용
    private const string SaveKey = "CurrentNode";


    //event 관리
    public event Action<GameState> OnGameStateChanged;

    private GameState _currentState;
    #endregion

    #region [initialization]

    // 게임 로직 초기화
    private void Start()
    {
        //이벤트 구독
        player.OnDied += OnCharacterDied;
        enemy.OnDied += OnCharacterDied;
    }
    #endregion


    #region 진행 관리

    //새로운 게임 시작
    public void StartNewGame(string startNodeName)
    {
        currentNodeName = startNodeName;
        SceneManager.LoadScene("GameWindow");
        StoryManager.Instance.StartNewProgress(currentNodeName);
    }

    //기존 게임 불러오기
    public void LoadGame()
    {
        currentNodeName = PlayerPrefs.GetString(SaveKey, "StartNode");
        SceneManager.LoadScene("GameWindow");
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

    #region [이벤트 관리]
    private void OnCharacterDied(Character character)
    {
        if (character is Player)
        {
            Debug.Log("플레이어 사망");
            //게임 오버 UI, 재시작, 엔딩 크레딧 등등.
        }
        else if (character is Enemy)
        {
            Debug.Log("적 사망");
            //보상, 다음 스테이지 이동.
        }
    }

    public void UpdateGameState(GameState newState)
    {
        _currentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    #endregion
}
