using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : SingleTon<GameManager>
{
    #region [변수 관리]
    [Header("플레이어 정보")]
    public PlayerData playerData;
    public ItemData itemData;

    [Header("게임 상태 정보")]
    private GameState currentState;



    public int goodAndEvil { get; private set; }
    #endregion

    #region [initialization]
    protected override void Awake()
    {
        base.Awake();
        playerData = new PlayerData();
        UpdateGameState(GameState.Main);
    }
    #endregion

    #region [이벤트 관리]
    public event Action<GameState> OnGameStateChanged;

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
    public void BackToMain()
    {
        GameEvent.SaveGame();
        UpdateGameState(GameState.Main);
        SceneManager.LoadScene("MainWindow");
    }

    public void StartNewGame()
    {
        UpdateGameState(GameState.New);
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
            if (currentState == GameState.New)
            {
                Debug.Log("새로운 세계를 시작하기 위해 핵을 떨구는 중입니다...");
                //코루틴을 이용한 로딩바 추가도 가능.

                //새로운 Data 생성.
                playerData = new PlayerData();
                itemData = new ItemData();
                ResetGoodAndEvil();
                //currencyManager 초기화 함수.
                CurrencyManager.Instance.InitializeManager();

                UpdateGameState(GameState.Playing);
                CharacterManager.Instance.SpawnCharacter(playerData, 0);
                InventoryManager.Instance.MakeNew(itemData); //???? 이거 언제 만들었지??
                //시작 노드 고정. 이것도 수정해야함.
                var node = Resources.Load<Node>($"Nodes/Main_01");
                

                NodeManager.Instance.GoToNode(node);
            }
            else if (currentState == GameState.Load)
            {
                Debug.Log("저장된 세계를 불러오는 중입니다...");

                //저장된 Data 로드
                SaveData data = SaveManager.Instance.LoadData();
                ResetGoodAndEvil();
                ChangeGoodAndEvil(data.goodAndEvil);

                this.playerData = data.playerData;
                this.itemData = data.itemData;
                
                CurrencyManager.Instance.currencyList = data.currencyList;

                Debug.Log($"{CurrencyManager.Instance.GetAmount("Gold")}가 존재함이 확인!");
                Debug.Log(data.currencyList.Count);

                foreach (CurrencyData d in data.currencyList)
                {
                    if (d == null)
                    {
                        Debug.Log("감지되지 않음.");
                    }
                    Debug.Log($"{d.Name}이름을 지닌 재화를 호출하였다. 잔액 : {d.Amount}");
                    CurrencyEvent.CurrencyChanged(d);
                }


                if (playerData.currentHP == 0)
                {
                    Debug.Log("죽은 플레이어를 불러올 수는 없다.");
                    BackToMain();
                    return;
                }
                
                UpdateGameState(GameState.Playing);
                CharacterManager.Instance.SpawnCharacter(playerData, 1);
                InventoryManager.Instance.MakeNew(itemData);
                NodeManager.Instance.GoToNode(data.currentNode);
            }
        }
    }

    // public void SaveGame()
    // {
    //     SaveData data = new SaveData();

    //     data.playerData = CharacterManagerInstance.currentPlayer.GetCurrentData();
    //     data.currentNode = NodeManager.Instance.currentNode;
    //     data.goodAndEvil = GameManager.goodAndEvil;
    //     data.currencyList = CurrencyManager.Instance.currencyList;
    //     data.itemData = InventoryManager.Instance.inventoryItems;

    //     SaveManager.Instance.SaveData(data);
    // }

    public void QuitGame()
    {
        Debug.Log("게임을 종료합니다...");

        //추후 가능하다면 세이브 완료 후 종료할 수 있도록 변경.
        Application.Quit();
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

    #region [선행, 악행 수치 관리]
    public void ChangeGoodAndEvil(int amount)
    {
        this.goodAndEvil += amount;
        //
    }
    public void ResetGoodAndEvil()
    {
        this.goodAndEvil = 0;
    }

    #endregion

}
