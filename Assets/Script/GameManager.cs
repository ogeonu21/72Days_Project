using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    #region [변수 관리]

    public static GameManager instance { get; private set; }

    // 게임 내의 다른 매니저들을 관리
    public Player player;
    public Enemy enemy;

    public int survive_data; // 생존 날짜

    //진행도 관리
    public string currentNodeName; // 진행 저장용
    private const string SaveKey = "CurrentNode";
    #endregion

    #region [initialization]

    // 중복 방지용 플래그
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 기존 인스턴스가 있을 경우 파괴
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 변경되어도 파괴되지 않게 설정
    }

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
    }

    //기존 게임 불러오기
    public void LoadGame()
    {
        currentNodeName = PlayerPrefs.GetString(SaveKey, "StartNode");
        SceneManager.LoadScene("GameWindow");
    }

    //진행도 저장
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

    #endregion
}
