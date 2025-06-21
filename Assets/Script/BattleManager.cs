using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



public class BattleManager : MonoBehaviour
{
    public string[] attackAreaArray = { "Head", "Body", "Arm", "Leg" };
    public string[] attackAreaKorean = {"머리", "몸", "팔", "다리" };
    public float[] hitRatioArray = { 40.0f, 90.0f, 70.0f, 80.0f };
    private float[] effectRatioArray = { 60.0f, 15.0f, 30.0f, 20.0f };
    private float[] attackRatioArray = { 1.6f, 0.7f, 0.9f, 1.0f };

    public enum States
    {
        Head,
        Body,
        Arm,
        Leg
    }

    public States playerAttackArea;
    public States enemyAttackArea;

    public Player player; //플레이어객체
    public Enemy enemy; //적

    public Character firstAttack;
    public Character secondAttack;
    public States firstAttackArea;
    public States secondAttackArea;

    private bool firstAttackSuccess = false;
    private bool secondAttackSuccess = false;
    private bool firstEffectSuccess = false;
    private bool secondEffectSuccess = false;

    private bool nextStep = false;

    //현재 턴수
    public int turn = 0;

    public bool isPlayerTurn; //플레이어 턴 여부

    //text Canvas
    public TextTwinkle textCanvas;

    private static BattleManager instance;

    public GameObject buttonCanvas;

    // 접근자 프로퍼티
    public static BattleManager Instance
    {
        get
        {
            if (instance == null)
            {
                // GameManager를 찾아보고 없으면 생성한다.
                instance = FindObjectOfType<BattleManager>();

                if (instance == null)
                {
                    GameObject gameManagerObject = new GameObject("GameManager");
                    instance = gameManagerObject.AddComponent<BattleManager>();
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


    public void battleStart()
    {
        buttonCanvas.SetActive(true);
        GameObject.Find("Player").transform.Find("ButtonCanvas").gameObject.SetActive(true);

        textCanvas.StartCoroutine(textCanvas.TextPrint("전투가 시작되었다.\n어떤 행동을 취할 것인가?"));
        
        isPlayerTurn = true;
        turn = 1;
    }

    public void turnStart(string playerSelectedAttackArea)
    {
        Debug.Log("턴 시작");
        enemyAttackArea = (States)Enum.Parse(typeof(States), attackAreaArray[(int)UnityEngine.Random.Range(0, 4)], true);
        playerAttackArea = (States)Enum.Parse(typeof(States), playerSelectedAttackArea, true);


        firstAttacker(playerAttackArea, enemyAttackArea);

        firstAttackSuccess = hitChance(firstAttackArea, secondAttack);



        //코루틴
        StartCoroutine(firstAttaking());
    }

    public void firstAttacker(States playerAttackArea, States enemyAttackArea)
    {
        if (player.attackDistance >= enemy.attackDistance)
        {
            firstAttack = player;
            firstAttackArea = playerAttackArea;
            secondAttack = enemy;
            secondAttackArea = enemyAttackArea;
        }
        else
        {
            firstAttack = enemy;
            firstAttackArea = enemyAttackArea;
            secondAttack = player;
            secondAttackArea = playerAttackArea;
        }
    }
    IEnumerator firstAttaking()
    {
        if (firstAttackSuccess)
        {
            yield return textCanvas.StartCoroutine(textCanvas.TextPrint($"{firstAttack.name}가 {attackAreaKorean[(int)firstAttackArea]}를 공격하였다."));
            
            if (firstAttack == player)
            {
                player.durabilityReduce();
            }
            else
            {
                player.StartCoroutine(player.BloodEffect());
            }
            yield return StartCoroutine(firstEffect());
            secondAttack.TakeDamage(attackRatioArray[(int)firstAttackArea] * firstAttack.attack);
            yield return textCanvas.StartCoroutine(textCanvas.TextPrintln($"{secondAttack.name}은 {attackRatioArray[(int)firstAttackArea] * firstAttack.attack}의 피해를 입었다."));
        }
        else
        {
            yield return textCanvas.StartCoroutine(textCanvas.TextPrint($"{firstAttack.name}가 {attackAreaKorean[(int)firstAttackArea]}을 공격하였으나 빗나갔다."));
        }


        yield return new WaitForSeconds(0.5f);
        if (firstAttack.hp > 0 && secondAttack.hp > 0)
        {
            yield return StartCoroutine(secondAttaking());
        }
        else
        {
            Debug.Log("게임 종료");
            yield return textCanvas.StartCoroutine(textCanvas.TextPrint("전투가 종료되었다."));
            if (player.hp <= 0)
            {
                Defeat();
            }
            else
            {
                Victory();
            }
        }
    }

    IEnumerator secondAttaking()
    {
        if (secondAttackSuccess)
        {
            yield return textCanvas.StartCoroutine(textCanvas.TextPrint($"{secondAttack.name}가 {attackAreaKorean[(int)secondAttackArea]}를 공격하였다."));
            if (secondAttack == player)
            {
                player.durabilityReduce();
            }
            else
            {
                player.StartCoroutine(player.BloodEffect());
            }
            yield return StartCoroutine(secondEffect());
            firstAttack.TakeDamage(attackRatioArray[(int)secondAttackArea] * secondAttack.attack);
            yield return textCanvas.StartCoroutine(textCanvas.TextPrintln($"{firstAttack.name}은 {attackRatioArray[(int)secondAttackArea] * secondAttack.attack}의 피해를 입었다."));
        }
        else
        {
            yield return textCanvas.StartCoroutine(textCanvas.TextPrint($"{secondAttack.name}가 {attackAreaKorean[(int)secondAttackArea]}을 공격하였으나 빗나갔다."));
        }
        yield return new WaitForSeconds(0.5f);
        if (firstAttack.hp > 0 && secondAttack.hp > 0) {
            yield return textCanvas.StartCoroutine(textCanvas.TextPrint($"어떤 행동을 취할 것인가?"));
        }
        else
        {
            Debug.Log("게임 종료");
            yield return textCanvas.StartCoroutine(textCanvas.TextPrint("전투가 종료되었다."));
            if (player.hp <= 0)
            {
                Defeat();
            }
            else
            {
                Victory();
            }
        }
        player.EndTurn();
        enemy.EndTurn();
        isPlayerTurn = true;

    }
    IEnumerator firstEffect()
    {
     
        firstEffectSuccess = ((UnityEngine.Random.Range(0.0f, 1.0f) * 100) <= effectRatioArray[(int)firstAttackArea]);
        if (firstEffectSuccess)
        {
            switch (firstAttackArea)
            {
                case States.Head:
                    //다음 턴의 적의 회피율이 떨어졌다는 메세지 출력
                 
                    yield return textCanvas.StartCoroutine(textCanvas.TextPrintln($"{firstAttack.name}의 공격으로 {secondAttack.name}가 뇌진탕을 입어 회피율이 2턴간 5%만큼 감소하였고,"));
                    secondAttack.ApplyEvasionDebuff(5.0f);

                    secondAttackSuccess = ((UnityEngine.Random.Range(0.0f, 1.0f) * 100) <= (hitRatioArray[(int)secondAttackArea] - firstAttack.avd));
                    break;
                case States.Body:
                    //다음 턴의 적의 사거리가 감소했다는 메세지 출력
                    secondAttackSuccess = ((UnityEngine.Random.Range(0.0f, 1.0f) * 100) <= (hitRatioArray[(int)secondAttackArea] - firstAttack.avd));
                    break;
                case States.Arm:
                    //적의 공격을 방어했다는 메세지 출력
                    
                    yield return textCanvas.StartCoroutine(textCanvas.TextPrintln($"{firstAttack.name}가 팔을 공격하여 {secondAttack.name}의 공격을 방어하였고,"));
                    secondAttackSuccess = false;
                    break;
                case States.Leg:
                    yield return textCanvas.StartCoroutine(textCanvas.TextPrintln($"{firstAttack.name}의 공격으로 다리 부상을 입은 {secondAttack.name}의 사거리가 2턴간 1만큼 감소하였고,"));
                    secondAttack.ApplyDistanceDebuff();
                    secondAttackSuccess = ((UnityEngine.Random.Range(0.0f, 1.0f) * 100) <= (hitRatioArray[(int)secondAttackArea] - firstAttack.avd));
                    break;
                default:
                    break;
            }
        }
        else
        {
            secondAttackSuccess = ((UnityEngine.Random.Range(0.0f, 1.0f) * 100) <= (hitRatioArray[(int)secondAttackArea] - firstAttack.avd));
        }

    }
    IEnumerator secondEffect()
    {
        
        secondEffectSuccess = ((UnityEngine.Random.Range(0.0f, 1.0f) * 100) <= effectRatioArray[(int)secondAttackArea]);
        if (secondEffectSuccess)
        {
            switch (secondAttackArea)
            {
                case States.Head:
                    //다음 턴의 적의 회피율이 떨어졌다는 메세지 출력
                    yield return textCanvas.StartCoroutine(textCanvas.TextPrintln($"{secondAttack.name}의 공격으로 {firstAttack.name}가 뇌진탕을 입어 회피율이 2턴간 5%만큼 감소하였고,"));
                    firstAttack.ApplyEvasionDebuff(5.0f);
                    break;
                case States.Body:

                    break;
                case States.Arm:
                    //적의 공격을 방어했다는 메세지 출력
                    yield return textCanvas.StartCoroutine(textCanvas.TextPrintln($"{secondAttack.name}가 {firstAttack.name}의 공격을 방어하려하였으나 실패하였고,"));
                    break;
                case States.Leg:
                    //다음 턴의 적의 사거리가 감소했다는 메세지 출력
                    yield return textCanvas.StartCoroutine(textCanvas.TextPrintln($"{secondAttack.name}의 공격으로 다리 부상을 입어 {firstAttack.name}의 사거리가 2턴간 1만큼 감소하였고,"));
                    firstAttack.ApplyDistanceDebuff();
                    break;
                default:
                    break;
            }
        }

    }

    public bool hitChance(States area, Character receiver)
    {
        float hitRatio = hitRatioArray[(int)area] - receiver.avd;
        float attackRatio = (UnityEngine.Random.Range(0.0f, 1.0f) * 100);
        bool hit = (attackRatio <= hitRatio);
        Debug.Log($"때릴 확률 : {attackRatio}, 맞을 확률 : {hitRatio}");

        return hit;
    }

    private void Victory()
    {
        buttonCanvas.SetActive(false);
        int reward = (UnityEngine.Random.Range(0, 4) + 1);
        textCanvas.StartCoroutine(textCanvas.TextPrint($"당신은 {enemy.name}으로부터 살아남았다.\n목숨을 건 전투로 인해 당신의 체력이 {reward}만큼 상승하였다."));
        
        //추가 보상 및 이벤트 메세지 출력 필요
        player.StatusPlus(reward);


        player.EquipWeapon(player.CreateWeapon("사시미"));

        nextStep = true;
        

        
        
    }
    private void Defeat()
    {
        buttonCanvas.SetActive(false);
        GameManager.Instance.EndGame();
        //다시하기, 종료하기
    }

    IEnumerator delaySeconds(float d)
    {
        yield return new WaitForSeconds(d);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (nextStep == true) {
                GameManager.Instance.Invoke("nextStage", 0);
                nextStep = false;
            }
        }
    }

}
