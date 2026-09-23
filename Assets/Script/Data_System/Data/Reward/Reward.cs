using System.Collections;
using System.Collections.Generic;
using AOT;
using UnityEngine;
[System.Serializable]
public struct Reward
{
    //보상 목록
    public BaseItem dropItem;
    [Min(0)] public float itemDropRate;
    [Min(0)] public int dropGold;
    [Min(0)] public int hpHeal;
    [Min(0)] public int exp;
    public BaseStats statIncrease;
    public List<ItemReward> items;
    //public BaseStats baseStats;

    public Reward(BaseItem dropItem, float itemDropRate, int dropGold, int hpHeal, int exp)
    {
        this.dropItem = dropItem;
        this.itemDropRate = itemDropRate;
        this.dropGold = dropGold;
        this.hpHeal = hpHeal;
        this.exp = exp;
        statIncrease = default;
        items = new List<ItemReward>();
    }
}

[System.Serializable]
public sealed class ItemReward
{
    public BaseItem item;
    [Min(1)] public int quantity = 1;
    [Range(0, 1)] public float probability = 1;
}

public sealed class RewardResult
{
    public bool success;
    public string message;
    public int healed;
    public readonly List<ItemReward> grantedItems = new List<ItemReward>();
}
