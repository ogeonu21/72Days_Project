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
    private CharacterManager characterSource;

    //Instance
    private Enemy enemy;
    private Player player;

    #endregion
    [Header("CombatSetting")]
    public bool combatActive;
    public bool onPlayerTurn;
    private PlayerInputData playerInputData;
    [SerializeField, Range(0f, 1f)] private float escapeProbability = .5f;
    private Node escapeNode;
    private string usedItemName;
    public bool CanChooseAction => combatActive && onPlayerTurn && player != null && !player.IsDead;
    public bool CanEscape => escapeNode != null;
    public float EscapeProbability => escapeProbability;

    [Header("CombatResult")]
    private Node successNode;
    private Node failureNode;
    #endregion

    #region [이벤트 그룹]
    public event Action<Player, Enemy> CombatUIUpdate;
    public event Action<bool> ActionSelectionChanged;
    #endregion

    #region [초기화]
    protected override void Awake()
    {
        base.Awake();
        gameManager = GameManager.Instance; //SaveGame을 위해 Instance를 저장. 굳이?
        characterSource = CharacterManager.Instance;
        if (characterSource != null) characterSource.OnCharacterReady += UpdateCharacter;
    }
    void OnDestroy()
    {
        if (characterSource != null)
        {
            characterSource.OnCharacterReady -= UpdateCharacter;
        }
    }

    private void UpdateCharacter(Player player, Enemy enemy)
    {
        this.enemy = enemy;
        this.player = player;
    }
    #endregion

    #region [Input Field]
    public void GetInput(PlayerInputData data)
    {
        TrySubmitInput(data, out _);
    }

    public bool TrySubmitInput(PlayerInputData data, out string error)
    {
        error = null;
        if (!CanChooseAction) { error = "지금은 행동을 선택할 수 없습니다."; return false; }
        if (!Enum.IsDefined(typeof(PlayerBehaviour), data.playerBehaviour))
        { error = "알 수 없는 행동입니다."; return false; }
        if (data.playerBehaviour == PlayerBehaviour.Run && !CanEscape)
        { error = "승리 후 이동 노드가 없어 도주할 수 없습니다."; return false; }
        if (data.playerBehaviour == PlayerBehaviour.Attack &&
            (player.areaDataDB == null || !Array.Exists(player.areaDataDB, area => area.Equals(data.areaData))))
        { error = "유효한 공격 부위를 선택하세요."; return false; }
        // 아이템 사용 알림 중 재진입하더라도 같은 턴을 중복 제출할 수 없다.
        onPlayerTurn = false;
        if (data.playerBehaviour == PlayerBehaviour.Use)
        {
            var inventory = InventoryManager.Instance;
            usedItemName = data.baseItem != null ? data.baseItem.itemName : "아이템";
            if (inventory == null || data.baseItem == null)
            { onPlayerTurn = true; error = "사용할 아이템이 없습니다."; return false; }
            int slot = inventory.inventoryItems.IndexOf(data.baseItem);
            if (!inventory.TryUseAt(slot, player, out error)) { onPlayerTurn = true; return false; }
        }
        playerInputData = data;
        ActionSelectionChanged?.Invoke(false);
        return true;
    }
    #endregion

    #region [CombatControl]
    public void CombatNodeStart(Node node)
    {
        CancelCombat();
        if (node != null && node.nodeType == NodeType.CombatNode)
        {
            StartCoroutine(LoadCombatNodeData(node as CombatNode));
        }
    }

    private IEnumerator LoadCombatNodeData(CombatNode node)
    {
        if (characterSource == null) characterSource = CharacterManager.Instance;
        if (characterSource != null) UpdateCharacter(characterSource.currentPlayer, characterSource.currentEnemy);
        if (node == null || player == null || enemy == null || node.enemyData == null)
        {
            Debug.LogError("[CombatManager] 플레이어, 적 또는 전투 노드 데이터가 없습니다.");
            yield break;
        }
        onPlayerTurn = false;
        ActionSelectionChanged?.Invoke(false);
        successNode = node.successNode;
        failureNode = node.failureNode;
        escapeNode = node.successNode;
        if (escapeNode == node) escapeNode = null;
        
        if (node.enemyData != null)
        {
            enemy?.InitializeFromData(node.enemyData);
        }
        else
        {
            Debug.LogWarning($"{node.combatEnemyID} : EnemyData UnFound");
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
        onPlayerTurn = false;
        combatActive = true;

        StartCoroutine(CombatLoopStart());
    }

    private IEnumerator CombatLoopStart()
    {
        Character[] who = {player, enemy};
        AreaData attackData;

        while (combatActive)
        {
            //선공 정하기
            int index = GetFirst();

            //effect효과 적용
            who[0].CountEffect();
            who[1].CountEffect();
            CombatUIUpdate?.Invoke(player, enemy);
            GameEvent.UpdateCharacterUI(player, enemy);

            //전투를 시작해도 되는가?
            if (!combatActive)
            {
                yield return StartCoroutine(CombatNodeEnd((player.IsDead) ? player : enemy));
                yield break;
            }

            //행동 전 입력 대기
            yield return GameEvent.OnNodeTextUpdate("무슨 행동을 할 것인가?");
            onPlayerTurn = true;
            ActionSelectionChanged?.Invoke(true);
            yield return new WaitUntil(() => !onPlayerTurn || !combatActive);
            if (!combatActive)
            {
                yield return CombatNodeEnd(player.IsDead ? (Character)player : enemy);
                yield break;
            }

            switch (playerInputData.playerBehaviour)
            {
                case PlayerBehaviour.Attack:
                    attackData = (index == 0) ? playerInputData.areaData : GetEnemyAttack();
                    yield return StartCoroutine(AttackTurn(who[index], who[(index + 1) % 2], attackData, index));

                    if (!combatActive)
                    {
                        yield return StartCoroutine(CombatNodeEnd((player.IsDead) ? player : enemy));
                        yield break;
                    }

            
                    //후공 전환을 위한 index 설정
                    index = (index + 1) % 2;
                    //공격 범위 설정.
                    attackData = (index == 0) ? playerInputData.areaData : GetEnemyAttack();
                    yield return StartCoroutine(AttackTurn(who[index], who[(index + 1) % 2], attackData, index));

                break;
                case PlayerBehaviour.Use:
                    yield return GameEvent.OnNodeTextUpdate(usedItemName + "을 사용하였다.");
                    yield return WaitForClick.WaitClick();
                    if (combatActive) yield return AttackTurn(enemy, player, GetEnemyAttack(), 1);
                break;
                case PlayerBehaviour.Run:
                    bool escaped = CombatRules.ResolveEscape(escapeProbability, UnityEngine.Random.value);
                    yield return GameEvent.OnNodeTextUpdate(escaped ? "전투에서 도주하였다." : "도주에 실패하였다.");
                    yield return WaitForClick.WaitClick();
                    if (escaped)
                    {
                        combatActive = false;
                        onPlayerTurn = false;
                        ActionSelectionChanged?.Invoke(false);
                        player.onDied -= CombatNodeStop;
                        enemy.onDied -= CombatNodeStop;
                        gameManager.playerData = player.GetCurrentData();
                        // 승리 보상이나 패배 처리를 거치지 않는다.
                        NodeManager.Instance.GoToNode(escapeNode);
                        yield break;
                    }
                    if (combatActive) yield return AttackTurn(enemy, player, GetEnemyAttack(), 1);
                break;
                default:
                    Debug.LogError("[CombatManager] GetInput 함수에서 잘못된 값을 입력받았습니다. CombatUIRouter를 확인해주세요.");
                    break;
            }
            //선공 턴
            
            
            //만약 선공 턴에서 전투가 끝났다면
            
            //전투가 끝이 났는가?
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
        onPlayerTurn = false;
        ActionSelectionChanged?.Invoke(false);
    }

    public void CancelCombat()
    {
        StopAllCoroutines();
        combatActive = false;
        onPlayerTurn = false;
        if (player != null) player.onDied -= CombatNodeStop;
        if (enemy != null) enemy.onDied -= CombatNodeStop;
        ActionSelectionChanged?.Invoke(false);
    }

    private IEnumerator CombatNodeEnd(Character take)
    {
        //Event로 바로 작동하는 것이 아닐, onDied가 발생하면 combatActive만 끄는 식으로.
        string logMessage = $"{take.characterName} {JosaUtility.GetJosa_이가(take.characterName)} 사망하였다. 전투가 종료되었다.";
        yield return GameEvent.OnNodeTextUpdate(logMessage);

        yield return StartCoroutine(WaitForClick.WaitClick());

        onPlayerTurn = false;

        if (enemy.IsDead) yield return StartCoroutine(RewardEvent.RewardCoroutine(enemy.reward));

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

    #endregion

    #region [Combat Function]
    private IEnumerator AttackTurn(Character who, Character take, AreaData where, int index)
    {
        CombatAttackResult result = CombatRules.ResolveAttack(
            who.AttackPower,
            where.damageMultiplier,
            where.hitRate,
            where.effectRate,
            take.DodgeRate,
            who.AccuracyRate,
            UnityEngine.Random.Range(0.95f, 1.05f),
            UnityEngine.Random.Range(0f, 1f),
            UnityEngine.Random.Range(0f, 1f));


        string logMessage;

        if (result.IsHit)
        {
            take.TakeDamage(result.Damage);
            logMessage = $"{who.characterName} {JosaUtility.GetJosa_은는(who.characterName)} {take.characterName}의 {where.label}을 공격하여 {result.Damage}의 피해를 입혔다.";


            //방어구 내구도 감소.
            if(take is Player)
            {
                Player p = take as Player;
                // 미착용 부위는 정상적인 빈 슬롯이다. 장착된 방어구만 소모한다.
                var armorSlots = p.equipmentData?.armorItem;
                if (armorSlots != null)
                    foreach (ArmorItem item in armorSlots)
                    {
                        if (item != null) item.Use(p);
                    }
            }
            //무기 내구도 감소
            if(who is Player)
            {
                Player p = who as Player;
                if(p.equipmentData != null && p.equipmentData.weaponItem != null)
                {
                    p.equipmentData.weaponItem.Use(p);
                }
            }
            if (result.AppliesEffect)
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
                return $"추가로, {take.characterName} {JosaUtility.GetJosa_은는(take.characterName)} 팔에 부상을 입어 다음 {Character.AttackDown_Turn} 턴간 공격이 {Character.AttackDown_Force}만큼 감소하였다.";
            case "다리":
                return $"추가로, {take.characterName} {JosaUtility.GetJosa_은는(take.characterName)} 다리에 부상을 입어 다음 {Character.DodgeDown_Turn} 턴간 회피율이 {Character.DodgeDown_Force:P0}만큼 감소하였다.";
            case "몸":
                return $"추가로, {take.characterName} {JosaUtility.GetJosa_은는(take.characterName)} 복부에 부상을 입어 다음 {Character.Bleeding_Turn} 턴간 {Character.Bleeding_Damage}의 출혈 피해를 추가로 입는다.";
            default:
                return string.Empty;
        }
    }
    #endregion
}
