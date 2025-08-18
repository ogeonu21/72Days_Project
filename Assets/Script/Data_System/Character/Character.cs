using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class ReadOnlyAttribute : PropertyAttribute { }

/// <summary>
/// 캐릭터 공통 베이스: 플레이어/적 공용
/// - BaseStats 입력 → DerivedStats 자동 계산
/// - 데미지, 회피 판정, 공격, 사거리 체크 등 기본 전투 유틸 포함
/// </summary>
/// 

public class Character : MonoBehaviour
{
    public string ID;
    public string characterName;

    #region [UI Component]
    [Header("UI Components")]
    public Slider hpSlider;
    public TMP_Text hpText;
    public TMP_Text nameText;
    #endregion

    #region [Stats]
    [Header("Base Stats")]
    public BaseStats baseStats;
    //이거는 캐릭터 ID를 통해서 읽어와야해.

    [Header("Tuning (보정치)")]
    [Tooltip("장비/버프 보정치")]
    public int attackBonus = 0;
    public int hpBonus = 0;
    public float dodgeBonus = 0.0f;
    public int rangeBonus = 0;

    [Header("Derived Stats")]
    [SerializeField, ReadOnly] protected DerivedStats derived;
    [SerializeField, ReadOnly] protected int currentHP;
    #endregion

    #region [Events]
    public event Action onDied;
    public event Action onStatsChanged; //Stats이 변경되었을때 작동
    public event Action<int, int> onHPChanged; //(currentHP, maxHP)
    
    #endregion

    #region [initialize]
    public int CurrentHP => currentHP;
    public int MaxHP => derived.maxHP;
    public int AttackPower => derived.attackPower;
    public float DodgeRate => derived.dodgeRate;
    public float AccuracyRate => derived.accuracyRate;
    public int AttackRange => derived.attackRange;
    
    public bool IsDead => currentHP <= 0;

    //Enemy를 읽기 위해 파싱값을 받아서 캐릭터 스테이터스를 적용하는 함수.

    //이거는 Enemy에 있어야하지 않을까.
    

    //자식 클래스에서는 event를 발생시킬 수 없기에, 별도의 함수가 필요.
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
        derived = new DerivedStats(baseStats.str, baseStats.dex, baseStats.con, attackBonus, hpBonus, dodgeBonus, rangeBonus);
        onStatsChanged?.Invoke();
        //UpdateUI추가 필요.
    }


    //Damage적용
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

        UpdateHP_UI();
    }

    protected virtual void Die()
    {
        onDied?.Invoke();
        // 필요시 애니/이펙트/비활성화 등
        // gameObject.SetActive(false);
    }

    #endregion

    #region [UI Update]
    private void UpdateHP_UI()
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
