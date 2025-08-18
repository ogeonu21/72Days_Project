using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public enum WhoAttack
{
    PlayerAttack,
    EnemyAttack
}

public class CombatManager : SingleTon<CombatManager>
{
    #region [변수 그룹]
    #region [기본 변수]
    //Manager
    private StoryManager storyManager;
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
    //전투의 활성화를 알림.
    public bool combatActive;
    //플레이어가 입력을 할 수 있냐 없냐.
    public bool onAttackTurn;

    [Header("CombatResult")]
    private StoryNode nextNode; 
    #endregion

    #region [이벤트 그룹]
    
    //combatTextUpdate Event
    public delegate IEnumerator CombatTextUpdate(string text);
    public event CombatTextUpdate onTextUpdate;

    public event Action OnCombatNodeChanged;
    public event Action<StoryNode> OnStoryNodeStart;
    public event Action<Character> CombatUIUpdate;

    #endregion

    #region [Initialize]
    protected override void Awake()
    {
        base.Awake();
        gameManager = GameManager.Instance;
        storyManager = StoryManager.Instance;
        CharacterManager.Instance.OnCharacterReady += UpdateCharacter;

        //Event 구독
        storyManager.OnCombatNodeStart += CombatNodeStart;
    }
    #endregion

    private void UpdateCharacter(Player player, Enemy enemy)
    {
        this.enemy = enemy;
        this.player = player;
    }

    private void CombatNodeStart(Choice node)
    {

        //3. 이벤트 구독. 다만 수정 필요. CombatNodeStop은 현재 보상 증정을 안하고 있으니, 보상을 증정하는 부분이 필요해.
        player.onDied += CombatNodeStop;
        enemy.onDied += CombatNodeStop;

        //4. 전투 시작.
        StartCoroutine(LoadData(node));
        combatActive = true;

    }

    private void CombatNodeStop()
    {
        if (player != null)
        {
            //전투가 끝나면 플레이어의 데이터를 다시 저장하는 방식. 다만 이 부분은 수정이 필요해보임.
            gameManager.playerData = player.GetCurrentData();
            player.onDied -= CombatNodeStop;
        }

        if (enemy != null)
        {
            enemy.onDied -= CombatNodeStop;
            enemy.gameObject.SetActive(false);
            //Destroy(enemy.gameObject);

            //이게 맞을까? onDied 이벤트의 구독을 종료하는 것은 맞지만, Destory는 안돼. enemy의 event를 구독하는 애들이 있잖아?
        }

        combatActive = false;
        onAttackTurn = false;

        gameManager.UpdateGameState(GameState.Story);
        OnStoryNodeStart?.Invoke(nextNode);
    }

    IEnumerator LoadData(Choice node)
    {
        nextNode = node.nextNode;
        string enemyID = node.combatEnemyID;
        var enemyData = Resources.Load<EnemyDefinition>($"NPCStats/{enemyID}");
        if (enemyData != null)
        {
            enemy?.InitializeFromDefinition(enemyData);
        }
        else
        {
            Debug.LogWarning($"{enemyID} : EnemyDefinition UnFound");
        }

        yield return onTextUpdate?.Invoke(enemy.characterName + "가 당신에게 싸움을 걸었다. \n 준비하라.");

        yield return new WaitForSeconds(1f);

        yield return onTextUpdate?.Invoke("무슨 행동을 할 것인가?");

        onAttackTurn = true;

        yield return null;
    }

    

    public void GetInput(AreaData data)
    {
        onAttackTurn = false;
        
        StartCoroutine(CombatLoopStart(data));
    }


    private IEnumerator CombatLoopStart(AreaData data)
    {
        where[0] = data;
        where[1] = GetEnemyAttack();
        who[0] = (Character)player;
        who[1] = (Character)enemy;

        int index = GetFirst();

        if (combatActive)
        {
            //첫번째 공격자의 공격
            yield return StartCoroutine(AttackTurn(who[index], who[(index + 1) % 2], where[index], index));
        }

        if (combatActive)
        {
            //두번째 공격자의 공격.
            index = (index + 1) % 2;
            yield return StartCoroutine(AttackTurn(who[index], who[(index + 1) % 2], where[index], index));
        }
        else
        {
            //누군가 죽었으니 전투를 중간에 종료해야한다.
            yield break;
        }
        
        //다음 턴으로 넘어가기
        onAttackTurn = true;
        //UI 업데이트 이게 아니지! 업데이트는 중간중간 해야하는거 아닌가? 여기는 수정이 필요하겟어.
        CombatUIUpdate?.Invoke(enemy);
        yield return onTextUpdate?.Invoke("무슨 행동을 할 것인가?");
    }


    //선공 확인 함수.
    //player랑 enemy간의 AttackRange 비교.
    private int GetFirst()
    {
        if (player.AttackRange >= enemy.AttackRange)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }


    //Enemy가 자신의 차례때 공격할 위치를 결정하는 함수.
    //나중에 Enemy에 넣고 함수를 조금 바꾸는게 좋을듯.
    private AreaData GetEnemyAttack()
    {
        int index = UnityEngine.Random.Range(0, 4);
        return AreaDataDB.All[index];
    }

    private IEnumerator AttackTurn(Character who, Character take, AreaData where, int index)
    {
        int damage = Mathf.RoundToInt(who.AttackPower * where.damageMultiplier);
        if (Roll(where.hitRate - take.DodgeRate + who.AccuracyRate))
        {
            if (Roll(where.effectRate))
            {
                Debug.Log("특수공격 추가 필요.");
                

                take.TakeDamage(damage);
                yield return onTextUpdate?.Invoke(take.characterName + "의 " + where.label + "을 공격하여 " + damage + "만큼의 피해를 입혔다.");
                yield return new WaitForSeconds(0.5f);
            }
            else
            {

                take.TakeDamage(damage);
                yield return onTextUpdate?.Invoke(take.characterName + "의 " + where.label + "을 공격하여 " + damage +"만큼의 피해를 입혔다.");
                yield return new WaitForSeconds(0.5f);
            }
        }
        else
        {
            yield return onTextUpdate?.Invoke(who.characterName + "은 " + take.characterName + "의 " + where.label + "을 공격하려하였으나, 공격이 빗나갔다.");
            yield return new WaitForSeconds(0.5f);
        }
     
        yield return null;
    }



    private bool Roll(float f)
    {
        return f >= UnityEngine.Random.Range(0f, 1f);
    }

}
