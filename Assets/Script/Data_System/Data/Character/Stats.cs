using System;
using UnityEngine;
using UnityEngine.Events;

#region [Data Containers]

[System.Serializable]
public struct BaseStats
{
    //레벨당 Stat +3.
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
public struct DerivedStats
{
    [ReadOnly] public int attackPower; //공격력
    [ReadOnly] public int maxHP; //최대 체력
    [ReadOnly] public float dodgeRate; // 회피율(0~1) 기본 회피율 0%
    [ReadOnly] public float accuracyRate; //명중률 (0~1)
    [ReadOnly] public int attackRange; // 공격 사거리 객체에 따라 상이
    

    //인자를 이렇게 받는게 아니라. BaseStats를 받는게 더 낫지 않을까?
    public DerivedStats(BaseStats baseStats, int atkBonus, int hpBonus, float dodgeBonus, int rangeBonus)
    {
        attackPower = 5 + baseStats.str * 3 + baseStats.dex * 1 + atkBonus;
        maxHP = 30 + baseStats.str * 5 + baseStats.con * 10 + hpBonus;
        dodgeRate = baseStats.dex * 0.02f + dodgeBonus;
        dodgeRate = Math.Clamp(dodgeRate, 0.00f, 0.7f);
        accuracyRate = baseStats.dex * 0.015f;
        attackRange = 0 + rangeBonus;
    }
}
#endregion
