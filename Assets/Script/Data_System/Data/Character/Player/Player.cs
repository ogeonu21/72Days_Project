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
        base.TakeDamage(amount);
       
        GameEvent.OnTakeDamage(currentHP, MaxHP);
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
            Heal(MaxHP);

            GameEvent.PlayerLevelUp();
        }
    }
    #endregion
}

