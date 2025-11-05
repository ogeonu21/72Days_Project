using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        UpdateLV_UI();
    }

    //객체별 피격 확률 변동을 위한 함수.
    public void AreaDataReset()
    {
        for (int i = 0; i < areaDataDB.Length; i++)
        {
            areaDataDB[i].hitRate = areaDataDB[i].hitRate * UnityEngine.Random.Range(0.8f, 1.2f);
        }
    }

    //보상 지급을 위한 경험치 Reward 계산.
    public int GetExpReward()
    {
        int x = Mathf.RoundToInt((baseStats.str + baseStats.dex + baseStats.con) / 3);
        //경험치 계산식.
        return  Mathf.RoundToInt(Mathf.Pow(x + 10, 2) / 12 + 2 * (x - 9) + 19);
    }

    //LV UI를 업데이트하는 함수.
    public void UpdateLV_UI()
    {
        //스탯 1당 레벨 1? 이거는 조정이 필요해보인다.
        int x = Mathf.RoundToInt((baseStats.str + baseStats.dex + baseStats.con));

        lvText.text = "LV." + x;
    }
}
