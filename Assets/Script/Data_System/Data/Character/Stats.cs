using System;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct BaseStats
{
    [Min(0)] public int str; //힘 1당 공격력 +3, 체력 +5
    [Min(0)] public int dex; //민첩 1당 공격력 +1, 회피율 + 2%, 명중률 + 1.5%;
    [Min(0)] public int con; //건강 1당 체력 +10, 체력 회복 +2

    public BaseStats(int str, int dex, int con)
    {
        this.str = str;
        this.dex = dex;
        this.con = con;
    }
}

[System.Serializable]
public struct Stats
{
    [Min(0)] public int str; //힘 1당 공격력 +3, 체력 +5
    [Min(0)] public int dex; //민첩 1당 공격력 +1, 회피율 + 2%, 명중률 + 1.5%;
    [Min(0)] public int con; //건강 1당 체력 +10, 체력 회복 +2

    [Min(1), ReadOnly] public int attackPower; //공격력
    [Min(10), ReadOnly] public int maxHP; //최대 체력
    [Range(0f, 1f), ReadOnly] public float dodgeRate; // 회피율(0~1) 기본 회피율 0%
    [Range(0f, 1f), ReadOnly] public float accuracyRate; //명중률 (0~1)
    [ReadOnly] public int attackRange; // 공격 사거리 객체에 따라 상이

    public Stats(BaseStats stats, TuningStats tuning)
    {
        this.str = stats.str;
        this.dex = stats.dex;
        this.con = stats.con;

        attackPower = 5 + str * 3 + dex * 1 + tuning.attackBonus;
        maxHP = 30 + str * 5 + con * 10 + tuning.hpBonus;
        dodgeRate = dex * 0.02f + tuning.dodgeBonus;
        dodgeRate = Math.Clamp(dodgeRate, 0.00f, 0.7f);
        accuracyRate = dex * 0.015f;
        attackRange = 0 + tuning.rangeBonus;
    }
}

[System.Serializable]
public struct TuningStats
{
    public int attackBonus; 
    public int hpBonus;
    public float dodgeBonus;
    public int rangeBonus;

    public TuningStats(int attackBonus, int hpBonus, float dodgeBonus, int rangeBonus)
    {
        this.attackBonus = attackBonus;
        this.hpBonus = hpBonus;
        this.dodgeBonus = dodgeBonus;
        this.rangeBonus = rangeBonus;
    }
}
