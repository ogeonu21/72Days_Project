using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : SingleTon<GameManager>
{
    #region [변수 관리]

    [Header("플레이어 정보")]
    public PlayerData playerData;

    [Header("게임 상태 정보")]
    public int survive_data; // 생존 날짜
    private GameState currentState;
    //아래 두 줄은 삭제 예정.
    public string currentNodeName; // 진행 저장용
    private const string SaveKey = "CurrentNode";

    #endregion

    #region [initialization]
    protected override void Awake()
    {
        base.Awake();
        playerData = new PlayerData();
    }
    #endregion

    #region [이벤트 관리]
    //GameStateChanged를 Notify할 Event
    public event Action<GameState> OnGameStateChanged;

    //SceneLoaded
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion


    #region [진행 관리]

    //새로운 게임 시작
    public void StartNewGame()
    {
        UpdateGameState(GameState.Start);
        SceneManager.LoadScene("GameWindow");
        
    }

    public void LoadGame()
    {
        UpdateGameState(GameState.Load);
        SceneManager.LoadScene("GameWindow");
        
        
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameWindow")
        {
            if (currentState == GameState.Start)
            {
                Debug.Log("새로운 세계를 시작하기 위해 핵을 떨구는 중입니다...");
                //코루틴을 이용한 로딩바 추가도 가능.

                //새로운 Data 생성.
                playerData = new PlayerData();
                UpdateGameState(GameState.Story);
                CharacterManager.Instance.SpawnCharacter(playerData, 0);


                StoryManager.Instance.StartNewProgress();
            }
            else if (currentState == GameState.Load)
            {
                Debug.Log("저장된 세계를 불러오는 중입니다...");

                //저장된 Data 로드
                SaveData data = SaveManager.Instance.LoadData();

                this.playerData = data.playerData;
                this.currentState = data.currentState;
                UpdateGameState(this.currentState);


                //근데!!! 여기서 만약에 Player가 죽어있다? 그러면 new GAme을 다시 시작하도록 해야함.
                CharacterManager.Instance.SpawnCharacter(playerData, 1);

                StoryManager.Instance.LoadProgress(data.currentNode);
            }
        }
    }

    public void SaveGame()
    {
        this.playerData = CharacterManager.Instance.currentPlayer.GetCurrentData();
        SaveData data = new SaveData();

        data.playerData = this.playerData;
        data.currentNode = StoryManager.Instance.currentNode;
        data.currentState = this.currentState;

        SaveManager.Instance.SaveData(data);
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
