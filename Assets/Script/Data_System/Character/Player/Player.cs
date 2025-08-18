using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Player : Character
{
    //각각의 글자, HP, 이름 등의 연결이 필요함.

    //데이터 리셋이 필요해.
    // Start is called before the first frame update
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

        UpdateStats();
        SetCurrentHPAndNotify(data.currentHP);
    }

    public PlayerData GetCurrentData()
    {
        PlayerData data = new PlayerData();

        // 1. 현재 객체가 보관하고 있던 baseStats와 보너스 값들을 그대로 전달
        data.baseStats = this.baseStats;
        data.attackBonus = this.attackBonus;
        data.hpBonus = this.hpBonus;
        data.dodgeBonus = this.dodgeBonus;
        data.rangeBonus = this.rangeBonus;

        // 2. Character의 기본 정보 전달
        data.displayName = this.characterName;

        // 3. 현재 상태 값 전달
        data.currentHP = this.currentHP;

        return data;
    }

    // Update is called once per frame
    public void UpdateData(Player newPlayer)
    {

        UpdateStats();
    }
}

