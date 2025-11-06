using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CombatManager : SingleTon<CombatManager>
{
    #region [변수 그룹]
    #region [기본 변수]
    //Manager
    private GameManager gameManager;
    private CombatUIController combatUIController;

    //Instance
    private Enemy enemy;
    private Player player;

    //Array
    AreaData[] where = new AreaData[2];
    Character[] who = new Character[2];
    private string[] areaIndex = { "머리", "몸", "팔", "다리" };

    #endregion
    [Header("CombatSetting")]
    public bool combatActive;
    public bool onAttackTurn;
    private AreaData playerInputData;

    [Header("CombatResult")]
    private Node successNode;
    private Node failureNode;
    #endregion

    #region [이벤트 그룹]
    public event Action<Player, Enemy> CombatUIUpdate;
    #endregion

    #region [초기화]
    protected override void Awake()
    {
        base.Awake();
        gameManager = GameManager.Instance; //SaveGame을 위해 Instance를 저장. 굳이?
        CharacterManager.Instance.OnCharacterReady += UpdateCharacter;
    }
    void OnDestroy()
    {
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnCharacterReady -= UpdateCharacter;
        }
    }

    private void UpdateCharacter(Player player, Enemy enemy)
    {
        this.enemy = enemy;
        this.player = player;
    }
    #endregion

    #region [Input Field]
    public void GetInput(AreaData data)
    {
        this.playerInputData = data;
        onAttackTurn = false;
    }
    #endregion

    #region [CombatControl]
    public void CombatNodeStart(Node node)
    {
        if (node.nodeType == NodeType.CombatNode)
        {
            StartCoroutine(LoadCombatNodeData(node as CombatNode));
        }
    }

    private IEnumerator LoadCombatNodeData(CombatNode node)
    {
        successNode = node.successNode;
        failureNode = node.failureNode;
        
        if (node.enemyData != null)
        {
            enemy?.InitializeFromDefinition(node.enemyData);
        }
        else
        {
            Debug.LogWarning($"{node.combatEnemyID} : EnemyDefinition UnFound");
        }

        CombatUIUpdate?.Invoke(player, enemy);
        GameEvent.UpdateCharacterUI(player, enemy);

        yield return GameEvent.OnNodeTextUpdate(enemy.characterName + JosaUtility.GetJosa_이가(enemy.characterName) + " 당신에게 싸움을 걸었다. \n 준비하라.");
        yield return StartCoroutine(WaitForClick.WaitClick());

        //이벤트 구독
        player.EffectReset();
        enemy.EffectReset();

        player.onDied += CombatNodeStop;
        enemy.onDied += CombatNodeStop;
        onAttackTurn = true;
        combatActive = true;

        StartCoroutine(CombatLoopStart());
    }

    private IEnumerator CombatLoopStart()
    {
        who[0] = player;
        who[1] = enemy;

        int index = GetFirst();

        while (combatActive)
        {
            who[0].CountEffect();
            who[1].CountEffect();
            CombatUIUpdate?.Invoke(player, enemy);
            GameEvent.UpdateCharacterUI(player, enemy);

            if (index == 0)
            {
                yield return GameEvent.OnNodeTextUpdate("무슨 행동을 할 것인가?");
                onAttackTurn = true;

                yield return new WaitUntil(() => onAttackTurn == false);
            }

            AreaData attackData = (index == 0) ? playerInputData : GetEnemyAttack();
            yield return StartCoroutine(AttackTurn(who[index], who[(index + 1) % 2], attackData, index));

            index = (index + 1) % 2;

            if (!combatActive)
            {
                yield return StartCoroutine(CombatNodeEnd((player.IsDead) ? player : enemy));
                yield break;
            }

        }
    }

    private void CombatNodeStop()
    {
        combatActive = false;
    }

    private IEnumerator CombatNodeEnd(Character take)
    {
        //Event로 바로 작동하는 것이 아닐, onDied가 발생하면 combatActive만 끄는 식으로.
        string logMessage = $"{take.characterName} {JosaUtility.GetJosa_이가(take.characterName)} 사망하였다. 전투가 종료되었다.";
        yield return GameEvent.OnNodeTextUpdate(logMessage);

        yield return StartCoroutine(WaitForClick.WaitClick());

        combatActive = false;
        onAttackTurn = false;

        if (enemy.IsDead) yield return StartCoroutine(RewardEvent.RewardCoroutine(enemy, player));

        //플레이어 데이터 저장.
        gameManager.playerData = player.GetCurrentData();

        //이벤트 구독 해제
        player.onDied -= CombatNodeStop;
        enemy.onDied -= CombatNodeStop;

        if (player.IsDead)
        {
            NodeManager.Instance.GoToNode(failureNode);
        }
        if (enemy.IsDead)
        {
            NodeManager.Instance.GoToNode(successNode);
        }
    }
    #endregion

    #region [Calculate Fucntion]
    //선공 확인 함수.
    //player랑 enemy간의 AttackRange 비교.
    private int GetFirst()
    {
        return (player.AttackRange >= enemy.AttackRange) ? 0 : 1;
    }

    //Enemy가 자신의 차례때 공격할 위치를 결정하는 함수.
    private AreaData GetEnemyAttack()
    {
        return enemy.areaDataDB[UnityEngine.Random.Range(0, 4)];
    }

    private bool Roll(float f)
    {
        return f >= UnityEngine.Random.Range(0f, 1f);
    }
    #endregion

    #region [Combat Function]
    private IEnumerator AttackTurn(Character who, Character take, AreaData where, int index)
    {
        //데미지 공식 = 공격자 공격력 * 공격부위 공격파워 * 
        int damage = Mathf.RoundToInt(who.AttackPower * where.damageMultiplier * UnityEngine.Random.Range(0.95f, 1.05f)); ;
        bool isHit = Roll(where.hitRate - take.DodgeRate + who.AccuracyRate);


        string logMessage;

        if (isHit)
        {
            take.TakeDamage(damage);
            logMessage = $"{who.characterName} {JosaUtility.GetJosa_은는(who.characterName)} {take.characterName}의 {where.label}을 공격하여 {damage}의 피해를 입혔다.";

            if (damage > 0 && Roll(where.effectRate))
            {   
                take.TakeEffect(where);
                logMessage += "\n" + GetEffectMessage(where, take);
            }
        }
        else
        {
            logMessage = $"{who.characterName} {JosaUtility.GetJosa_은는(who.characterName)} {take.characterName}의 {where.label}을 공격하려 하였으나, 빗나갔다.";
        }

        yield return GameEvent.OnNodeTextUpdate(logMessage);
        yield return StartCoroutine(WaitForClick.WaitClick());

        yield return null;
    }

    private string GetEffectMessage(AreaData where, Character take)
    {
        switch (where.label)
        {
            case "팔":
                return $"추가로, {take.characterName} {JosaUtility.GetJosa_은는(take.characterName)} 팔에 부상을 입어 다음 두 턴간 공격이 5만큼 감소하였다.";
            case "다리":
                return $"추가로, {take.characterName} {JosaUtility.GetJosa_은는(take.characterName)} 다리에 부상을 입어 다음 두 턴간 회피율이 5%만큼 감소하였다.";
            case "몸":
                return $"추가로, {take.characterName} {JosaUtility.GetJosa_은는(take.characterName)} 복부에 부상을 입어 다음 두 턴간 3의 출혈 피해를 추가로 입는다.";
            default:
                return string.Empty;
        }
    }
    #endregion
}
