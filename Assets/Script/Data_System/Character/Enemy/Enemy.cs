using System.Collections;
using System.Collections.Generic;
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

        UpdateStats();
        SetCurrentHPAndNotify(MaxHP);
    }
}
