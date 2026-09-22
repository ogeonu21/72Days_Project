using System.Collections;
using System.Collections.Generic;
using AOT;
using UnityEngine;
public struct Reward
{
    //보상 목록
    public BaseItem dropItem;
    [Min(0)] public float itemDropRate;
    [Min(0)] public int dropGold;
    [Min(0)] public int hpHeal;
    [Min(0)] public int exp;
    //public BaseStats baseStats;

    public Reward(BaseItem dropItem, float itemDropRate, int dropGold, int hpHeal, int exp)
    {
        this.dropItem = dropItem;
        this.itemDropRate = itemDropRate;
        this.dropGold = dropGold;
        this.hpHeal = hpHeal;
        this.exp = exp;
    }
}
