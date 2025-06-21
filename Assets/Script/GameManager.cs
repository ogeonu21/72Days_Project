using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StatusData
{
    public string type; //캐릭터 타입
    public string name; //캐릭터 이름
    public int str; //힘 스탯
    public int dex; //민첩 스탯
    public int hpState; //체력 스탯
}

[System.Serializable]
public class CharacterDataCollection
{
    public List<StatusData> characters; // 캐릭터 리스트
}

[System.Serializable]
public class StageEnemyName
{
    public string name;
}

[System.Serializable]
public class StageDataCollection
{
    public List<StageEnemyName> stages;
}

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static GameManager instance;

    // 접근자 프로퍼티
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                // GameManager를 찾아보고 없으면 생성한다.
                instance = FindObjectOfType<GameManager>();

                if (instance == null)
                {
                    GameObject gameManagerObject = new GameObject("GameManager");
                    instance = gameManagerObject.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

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

    // 게임 내의 다른 매니저들을 관리
    public Player player;
    public Enemy enemy;

    //스테이지 정보
    public StageDataCollection stageDatas;
    public CharacterDataCollection characterDataCollection;

    public int survive_data; // 생존 날짜ㅁ

    // 게임 로직 초기화
    private void Start()
    {
        //new game을 눌렀다면, 초기화
        InitializeManagers();
        LoadStageData();
        LoadStatusData();
        


        player.ReLoadObject();
        enemy.ReLoadObject();

        StartGame();
    }
    public void LoadStageData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("StageData");

        if (jsonFile != null)
        {
            // JSON 파일을 CharacterDataCollection 객체로 파싱
            stageDatas = JsonUtility.FromJson<StageDataCollection>(jsonFile.text);   
        }
        else
        {
            Debug.LogError("CharacterStatus.json file not found in Resources.");
        }
    }

    public void LoadStatusData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("CharacterStatus");

        if (jsonFile != null)
        {
            // JSON 파일을 CharacterDataCollection 객체로 파싱
            characterDataCollection = JsonUtility.FromJson<CharacterDataCollection>(jsonFile.text);
        }
        else
        {
            Debug.LogError("CharacterStatus.json file not found in Resources.");
        }
    }

    // 다른 매니저들 초기화
    private void InitializeManagers()
    {
       
    }

    public int currentStageNumber = 0;

    // 예시: 게임을 시작하는 함수
    public void LoadNewGame()
    {
        //듀토리얼 스테이지 실행

    }

    public void StartGame()
    {
        BattleManager.Instance.battleStart();
        // 게임 시작 로직 추가
    }
    public void nextStage()
    {
        enemy.ReLoadObject();
        StartGame();
    }

    // 예시: 게임 종료 함수
    public void EndGame()
    {
        Debug.Log("Game ended");
        // 게임 종료 로직 추가
    }
}

class StageControll
{
    public void stage()
    {

    }
}
