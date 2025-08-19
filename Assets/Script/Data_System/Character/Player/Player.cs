using System;
using UnityEngine;

public class Player : Character
{
    private const float BASE_EXP = 12.1f;
    private const float EXP_GROWTH_RATE = 1.33f;

    private int exp;
    public int lv;
    //public event Action OnPlayerLvUp;

    #region [Initialize]
    public void InitializeFromData(PlayerData data)
    {
        if (data == null) return;

        ID = string.IsNullOrWhiteSpace(data.id) ? ID : data.id;
        characterName = string.IsNullOrWhiteSpace(data.displayName) ? characterName : data.displayName;

        baseStats = data.baseStats;
        attackBonus = data.attackBonus;
        hpBonus = data.hpBonus;
        dodgeBonus = data.dodgeBonus;
        rangeBonus = data.rangeBonus;

        exp = data.exp;
        lv = data.lv;

        UpdateStats();
        SetCurrentHPAndNotify(MaxHP);
    }

    public void LoadFromData(PlayerData data)
    {
        if (data == null) return;

        ID = string.IsNullOrWhiteSpace(data.id) ? ID : data.id;
        characterName = string.IsNullOrWhiteSpace(data.displayName) ? characterName : data.displayName;

        baseStats = data.baseStats;
        attackBonus = data.attackBonus;
        hpBonus = data.hpBonus;
        dodgeBonus = data.dodgeBonus;
        rangeBonus = data.rangeBonus;
        exp = data.exp;
        lv = data.lv;

        UpdateStats();
        SetCurrentHPAndNotify(data.currentHP);
    }
    #endregion

    #region [Data]
    public PlayerData GetCurrentData()
    {
        PlayerData data = new PlayerData();

        data.baseStats = this.baseStats;
        data.attackBonus = this.attackBonus;
        data.hpBonus = this.hpBonus;
        data.dodgeBonus = this.dodgeBonus;
        data.rangeBonus = this.rangeBonus;
        data.displayName = this.characterName;
        data.currentHP = this.currentHP;
        data.exp = this.exp;
        data.lv = this.lv;

        return data;
    }
    #endregion

    #region [Override]
    public override void TakeDamage(int amount)
    {
        if (IsDead) return;
        currentHP = Mathf.Max(0, currentHP - amount);

        GameEvent.OnTakeDamage(currentHP, MaxHP);
        UpdateHP_UI();

        if (currentHP <= 0)
        {
            Die();
        }
        //Player의 경우에는 입은 데미지에 따라 피격이펙트.
    }
    #endregion

    #region [LV Control]
    public void GetExp(int exp)
    {
        this.exp += exp;
        UpdateLv();
    }

    private void UpdateLv()
    {
        int requiredExpForLvUP = Mathf.RoundToInt(BASE_EXP * Mathf.Pow(EXP_GROWTH_RATE, lv + 1));
        if (exp >= requiredExpForLvUP)
        {
            exp -= requiredExpForLvUP;
            lv++;
            //OnPlayerLvUp?.Invoke();
            //지금은 임시로 스탯 하나 올리기.
            int i = UnityEngine.Random.Range(1, 4);
            switch (i)
            {
                case 1:
                    baseStats.str++;
                    Debug.Log("힘증가");
                    UpdateStats();
                    break;
                case 2:
                    baseStats.dex++;
                    Debug.Log("민첩증가");
                    UpdateStats();
                    break;
                case 3:
                    baseStats.con++;
                    Debug.Log("건강증가");
                    UpdateStats();
                    break;
                default:
                    break;
            }
        }
    }
    #endregion
}

