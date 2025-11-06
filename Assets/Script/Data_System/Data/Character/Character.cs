using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class ReadOnlyAttribute : PropertyAttribute { }

public class Character : MonoBehaviour
{
    [Header("캐릭터 아이디")]
    public string ID;
    public string characterName;

    #region [UI Component]
    [Header("UI 요소")]
    public Slider hpSlider;
    public TMP_Text hpText;
    public TMP_Text nameText;
    public TMP_Text lvText;
    #endregion

    #region [Stats]
    [Header("기본 스탯")]
    public BaseStats baseStats;
    private Stats stats;

    [Header("보정치")]
    [Tooltip("장비/버프 보정치")]
    public TuningStats tuningStats;

    [Header("현재 체력")]
    [SerializeField, ReadOnly] protected int currentHP;
    #endregion

    #region [AreaData]
    //개별 부위 데이터베이스 => Enemy 객체에서 개별 확률 계산을 위해 적용.
    public AreaData[] areaDataDB = {
        new AreaData("머리", 0.4f, 0.6f, 1.6f),
        new AreaData("몸", 0.9f, 0.15f, 0.7f),
        new AreaData("팔", 0.65f, 0.3f, 1.1f),
        new AreaData("다리", 0.8f, 0.2f, 0.9f)
    };
    #endregion

    #region [Effect]
    //기존 스탯 보관용
    private float tempDodgeRate;
    private int tempAttackPower;

    private int[] EffectTurn = new int[4];
    //기존 회피율 등등이 필요함.
    #endregion

    #region [Events]
    public event Action onDied;
    public event Action onStatsChanged; //Stats이 변경되었을때 작동 -> 아직은 연결된 곳 없음.
    #endregion

    #region [initialize]
    //읽기 전용.
    //해야하나??
    public int CurrentHP => currentHP;
    public int MaxHP => stats.maxHP;
    public int AttackPower => stats.attackPower;
    public float DodgeRate => stats.dodgeRate;
    public float AccuracyRate => stats.accuracyRate;
    public int AttackRange => stats.attackRange;
    
    public bool IsDead => currentHP <= 0;
    
    //HP 수정.
    protected void SetCurrentHPAndNotify(int currentHP)
    {
        this.currentHP = currentHP;
        UpdateHP_UI();
    }
    #endregion

    #region [State Update Function]
    //Stats Update
    //장비 변경, 스탯 성장시에 작동.
    public void UpdateStats()
    {

        //스탯 변동시 체력회복을 위해서.
        int tmpMaxHP = MaxHP;
        stats = new Stats(baseStats, tuningStats);

        //디버프 해제시 스탯을 정상 적용하기 위해서.
        tempAttackPower = AttackPower;
        tempDodgeRate = DodgeRate;

        //스탯 변화 이벤트 발생.
        onStatsChanged?.Invoke();
        //최대체력 변화에 따른 현재체력 보정.
        Heal(MaxHP - tmpMaxHP);
        UpdateHP_UI();

    }
    #endregion

    #region [Combat Function]

    public virtual void TakeDamage(int amount)
    {
        if (IsDead) return;
        currentHP = Mathf.Max(0, currentHP - amount);

        UpdateHP_UI();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    //Item 사용시 적용하기 위한 Heal 함수.
    public virtual void Heal(int amount)
    {
        if (IsDead) return;
        currentHP = Mathf.Min(MaxHP, currentHP + Mathf.Max(0, amount));
        Debug.Log($"{amount}만큼의 체력을 회복하였다.");

        UpdateHP_UI();
    }

    protected virtual void Die()
    {
        onDied?.Invoke();
        // 필요시 애니/이펙트/비활성화 등
        // gameObject.SetActive(false);
    }
    #endregion

    #region [Effect Function]

    //특수 효과 턴수 계산.
    //특수 효과 턴이 존재한다면 -1, 만약 -1하여 0이 된다면 효과 해제.
    public void CountEffect()
    {
        for (int i = 0; i < 3; i++)
        {
            if (EffectTurn[i] > 0)
            {
                EffectTurn[i]--;

                switch (i)
                {
                    case 0:
                        if (EffectTurn[0] == 0)
                        {
                            tuningStats.attackBonus += 5;
                        }
                        break;
                    case 1:
                        if (EffectTurn[1] == 0)
                        {
                            tuningStats.dodgeBonus += 0.05f;
                        }
                        break;
                    case 2:
                    //복부 특수효과, 3의 데미지
                        TakeDamage(3);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                continue;
            }   
        }
        UpdateStats();
    }

    public void TakeEffect(AreaData data)
    {
        switch (data.label)
        {
            case "팔":
                if(EffectTurn[0] <= 0)
                {
                    tuningStats.attackBonus -= 5;
                }
                
                EffectTurn[0] = 3;
                break;
            case "다리":
                if(EffectTurn[1] <= 0)
                {
                    tuningStats.dodgeBonus -= 0.05f;
                }

                EffectTurn[1] = 3;
                break;
            case "몸":
                EffectTurn[2] = 2;
                break;
            default:
                break;
        }

        UpdateStats();
//이펙트 효과 적용 필요.
    }

    public void EffectReset()
    {
        if(EffectTurn[0] > 0)
        {
            tuningStats.attackBonus += 5;
        }
        if(EffectTurn[1] > 0)
        {
            tuningStats.dodgeBonus += 0.05f;
        }

        for (int i = 0; i < 4; i++)
        {
            EffectTurn[i] = 0;
        }
        UpdateStats();
    }
    #endregion

    #region [UI Update]
    protected void UpdateHP_UI()
    {
        nameText.text = characterName;

        if (hpSlider != null && MaxHP > 0)
        {
            hpSlider.value = (float)currentHP / MaxHP;
        }
        if (hpText != null)
        {
            hpText.text = $"{currentHP} / {MaxHP}";
        }
    }
    #endregion



    //이제 여기에 공격처리, 추가효과 적용 등을 구현.
    //사거리 판별은 combatManager에서 구현.
}
