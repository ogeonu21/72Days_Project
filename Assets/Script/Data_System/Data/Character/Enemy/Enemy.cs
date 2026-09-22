using System;
using UnityEngine;

public class Enemy : Character
{
    public BaseItem dropItem;
    public float itemDropRate;
    public int dropGold;
    private TuningStats definitionTuning;
    private AreaData[] initialAreas;

    public Reward reward{get; private set;}

    public void InitializeFromData(EnemyData data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        InitializeCharacter(data.id, data.displayName, data.baseStats, data.tuningStats);
        definitionTuning = data.tuningStats;

        //드랍 아이템과 보상 설정;
        reward = new Reward(data.dropItem, (data.dropItem != null ? Mathf.Clamp01(data.itemDropRate) : 0), data.dropGold, 0, GetExpReward());

        AreaDataReset();

        UpdateStats();

        SetCurrentHPAndNotify(MaxHP);
        UpdateLV_UI();
    }

    //객체별 피격 확률 변동을 위한 함수.
    private void AreaDataReset()
    {
        if (initialAreas == null) initialAreas = (AreaData[])areaDataDB.Clone();
        areaDataDB = (AreaData[])initialAreas.Clone();
        for (int i = 0; i < areaDataDB.Length; i++)
        {
            areaDataDB[i].hitRate = areaDataDB[i].hitRate * UnityEngine.Random.Range(0.9f, 1.1f);
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

        if (lvText != null) lvText.text = "LV." + x;
    }

    public override void UpdateTuningStats()
    {
        //특수 효과에 따른 스탯 조정.
        tuningStats = ApplyStatusEffects(definitionTuning);

        //최종 스탯 업데이트.
        UpdateStats();
    }
}
