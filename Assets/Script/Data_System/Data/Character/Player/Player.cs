using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        //여기까지는 동일한데?

        exp = data.exp;
        lv = data.lv;

        UpdateStats();
        SetCurrentHPAndNotify(MaxHP);
        UpdateLV_UI();
    }


    //인자를 하나 더 받자. initialmode, loadmode.
    
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
        UpdateLV_UI();
    }
    #endregion

    #region [Data]
    //현재 플레이어 정보를 저장하기 위한 함수.
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

    //데미지 피격 함수.
    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
       
        GameEvent.OnTakeDamage(currentHP, MaxHP);
    }
    #endregion

    #region [LV Control]

    //경험치를 올리는 함수.
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

            //10만큼 회복.
            Heal(10);


            UpdateLV_UI();

            //레벨업 이벤트 발생.
            GameEvent.PlayerLevelUp();
        }
    }

    private void UpdateLV_UI()
    {
        lvText.text = "Lv." + lv;
    }
    #endregion
}

