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
                Debug.Log("<color=red>[GameManager] </color>새로운 세계를 시작하기 위해 핵을 떨구는 중입니다...");
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
                Debug.Log("<color=red>[GameManager] </color>저장된 세계를 불러오는 중입니다...");

                //저장된 Data 로드
                if (!SaveManager.Instance.TryRestoreGame(
                        out PlayerData loadedPlayerData,
                        out ItemData loadedItemData,
                        out List<CurrencyData> loadedCurrencies,
                        out int loadedGoodAndEvil,
                        out Node loadedNode,
                        out EquipmentData loadedEquipmentData,
                        out string loadError))
                {
                    Debug.LogWarning($"[GameManager] 저장 게임을 불러오지 못했습니다: {loadError}");
                    UpdateGameState(GameState.Main);
                    SceneManager.LoadScene("MainWindow");
                    return;
                }

                ResetGoodAndEvil();
                ChangeGoodAndEvil(loadedGoodAndEvil);

                this.playerData = loadedPlayerData;
                this.itemData = loadedItemData;
                
                CurrencyManager.Instance.currencyList = loadedCurrencies;

                Debug.Log($"<color=red>[GameManager] </color> 돈 {CurrencyManager.Instance.GetAmount("Gold")} 원이 존재함이 확인되었습니다");

                foreach (CurrencyData d in loadedCurrencies)
                {
                    if (d == null)
                    {
                        Debug.Log("<color=red>[GameManager] </color>감지되지 않음.");
                    }
                    Debug.Log($"<color=red>[GameManager] </color>{d.Name}이름을 지닌 재화를 호출하였다. 잔액 : {d.Amount}원");
                    CurrencyEvent.CurrencyChanged(d);
                }


                if (playerData.currentHP == 0)
                {
                    Debug.Log("<color=red>[GameManager] </color>죽은 플레이어를 불러올 수는 없다.");
                    BackToMain();
                    return;
                }
                
                UpdateGameState(GameState.Playing);
                CharacterManager.Instance.SpawnCharacter(playerData, 1);
                CharacterManager.Instance.currentPlayer.RestoreEquipment(loadedEquipmentData);
                InventoryManager.Instance.MakeNew(itemData);
                NodeManager.Instance.GoToNode(loadedNode);
            }
        }
    }

    public void QuitGame()
    {
        Debug.Log("<color=red>[GameManager] </color>게임을 종료합니다...");

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
