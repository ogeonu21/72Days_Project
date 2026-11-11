using System;
using UnityEngine;

public class Enemy : Character
{
    /// <summary>실제 체력이 감소했을 때 피격 연출에 알린다. 출혈과 치명타도 포함한다.</summary>
    public event Action Damaged;

    public override void TakeDamage(int amount)
    {
        int previousHP = CurrentHP;
        base.TakeDamage(amount);
        if (CurrentHP < previousHP) Damaged?.Invoke();
    }

    public BaseItem dropItem;
    public float itemDropRate;
    public int dropGold;
    private TuningStats definitionTuning;
    public Reward reward{get; private set;}
    private AreaData[] initialAreas;
    // 잘못된 일차 입력이 수억 회 분배를 실행하지 않도록 보정 포인트를 제한한다.
    private const int MaxDayStatPoints = 100000;

    //현재 게임 진행률에 따라 스탯에 보정치를 가함.
    private static BaseStats EnemyStatsDayTuning(BaseStats baseStats, int surviveDay, Func<float> roll = null)
    {
        BaseStats tunedStats = baseStats;
        // 일차×0.7의 버림을 정수 연산으로 계산한다. 10일차는 정확히 7포인트다.
        long points = (long)Math.Max(0, surviveDay) * 7 / 10;
        long headroom = (long)int.MaxValue - Math.Max(baseStats.str, Math.Max(baseStats.dex, baseStats.con));
        int count = (int)Math.Min(points, Math.Min(MaxDayStatPoints, headroom));
        int[] bonus = StatDistribution(count, baseStats, roll);
        tunedStats.str += bonus[0];
        tunedStats.dex += bonus[1];
        tunedStats.con += bonus[2];
        return tunedStats;
    }
    //EnemyData를 기반으로 Enemy를 초기화하는 함수.

    public void InitializeFromData(EnemyData data, int surviveDay = 0)
    {
        //26.11.3
        //현재 EnemyData를 기반으로 Enemy를 그대로 생성하지만.
        //날짜에 계수를 더하여 스탯 총합을 랜덤하게 비율에 맞게 더해보려고 함. 계수는 향후 EnemyData에 추가할 예정이고 현재는 0.7로 해볼 예정.
        //계산식은 그러면 기본 데이터 + 날짜 * 0.7을 스탯 배분 비율에 맞추어 분배. 계산은 버림으로 정수만 적용.
        if (data == null) throw new ArgumentNullException(nameof(data));

        //baseStats에 보정을 가해야겠지?
        InitializeCharacter(data.id, data.displayName, EnemyStatsDayTuning(data.baseStats, surviveDay), data.tuningStats);
        definitionTuning = data.tuningStats;
        lv = CalculateLevel(baseStats);

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
        if (lvText != null) lvText.text = "LV." + lv;
    }

    public override void UpdateTuningStats()
    {
        //특수 효과에 따른 스탯 조정.
        tuningStats = ApplyStatusEffects(definitionTuning);

        //최종 스탯 업데이트.
        UpdateStats();
    }

    private static int CalculateLevel(BaseStats baseStats)
    {
        long sum = Math.Max(0L, (long)baseStats.str + baseStats.dex + baseStats.con);
        return (int)Math.Min(int.MaxValue, 1L + sum / 2);
    }

    private static int[] StatDistribution(int totalStatPoints, BaseStats baseStats, Func<float> roll = null)
    {
        var distribution = new int[3];
        if (totalStatPoints <= 0) return distribution;
        long str = Math.Max(0, baseStats.str), dex = Math.Max(0, baseStats.dex), con = Math.Max(0, baseStats.con);
        long total = str + dex + con;
        if (total == 0) { str = dex = con = 1; total = 3; }
        double strLimit = (double)str / total;
        double dexLimit = (double)(str + dex) / total;
        roll = roll ?? (() => UnityEngine.Random.value);
        for (int i = 0; i < totalStatPoints; i++)
        {
            float sample = roll();
            int index = sample < strLimit ? 0 : sample < dexLimit ? 1 : 2;
            // Random.value는 1도 반환한다. 가중치0인 마지막 부위에는 배분하지 않는다.
            if (index == 2 && con == 0) index = dex > 0 ? 1 : 0;
            distribution[index]++;
        }
        return distribution;
    }

}
