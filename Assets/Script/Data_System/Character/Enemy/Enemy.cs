using System;
using UnityEngine;

public class Enemy : Character
{

    public void InitializeFromDefinition(EnemyDefinition def)
    {
        if (def == null) return;

        ID = string.IsNullOrWhiteSpace(def.id) ? ID : def.id;
        characterName = string.IsNullOrWhiteSpace(def.displayName) ? characterName : def.displayName;

        baseStats = def.baseStats;
        attackBonus = def.attackBonus;
        hpBonus = def.hpBonus;
        dodgeBonus = def.dodgeBonus;
        rangeBonus = def.rangeBonus;

        AreaDataReset();
        
        UpdateStats();
        SetCurrentHPAndNotify(MaxHP);
    }

    public void AreaDataReset()
    {
        for (int i = 0; i < areaDataDB.Length; i++)
        {
            areaDataDB[i].hitRate = areaDataDB[i].hitRate * UnityEngine.Random.Range(0.8f, 1.2f);
        }
    }

    public int GetExpReward()
    {
        int x = Mathf.RoundToInt((baseStats.str + baseStats.dex + baseStats.con) / 3);
        return  Mathf.RoundToInt(Mathf.Pow(x + 10, 2) / 12 + 2 * (x - 9) + 19);
    }
}
