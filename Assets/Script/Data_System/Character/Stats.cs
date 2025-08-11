using System;
using UnityEngine;
using UnityEngine.Events;

#region [Data Containers]

[System.Serializable]
public struct BaseStats
{
    [Min(0)] public int str; //힘 1당 공격력 +3, 체력 +5
    [Min(0)] public int dex; //민첩 1당 공격력 +2, 회피율 + 0.3%
    [Min(0)] public int con; //건강 1당 체력 +10, 체력 회복 +2
}

[System.Serializable]
public struct DerivedStats
{
    [ReadOnly] public int attackPower; //공격력
    [ReadOnly] public int maxHP; //최대 체력
    [ReadOnly] public float dodgeRate; // 회피율(0~1) 기본 회피율 5%
    [ReadOnly] public int attackRange; // 공격 사거리 객체에 따라 상이

    public DerivedStats(int str, int dex, int con, int atkBonus, int hpBonus, float dodgeBonus, int rangeBonus)
    {
        attackPower = 5 + str * 3 + dex * 2 + atkBonus;
        maxHP = 30 + str * 5 + con * 10 + hpBonus;
        dodgeRate = 0.05f + dex * 0.003f + dodgeBonus;
        dodgeRate = Math.Clamp(dodgeRate, 0.00f, 0.7f);
        attackRange = 0 + rangeBonus;
    }
}
#endregion
