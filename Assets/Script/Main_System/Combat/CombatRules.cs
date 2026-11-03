using System;

/// <summary>
/// Unity 생명주기와 분리된 전투 판정 규칙입니다.
/// 난수는 호출자가 주입하므로 동일 입력에 대해 항상 같은 결과를 만듭니다.
/// </summary>
public static class CombatRules
{
    public static bool ResolveEscape(float probability, float roll)
    {
        if (float.IsNaN(probability) || probability <= 0f) return false;
        if (probability >= 1f) return true;
        return roll < probability;
    }
    public static CombatAttackResult ResolveAttack(
        int attackPower,
        float damageMultiplier,
        float hitRate,
        float effectRate,
        float targetDodgeRate,
        float attackerAccuracyRate,
        float damageVariance,
        float hitRoll,
        float effectRoll)
    {
        int damage = Math.Max(0, (int)Math.Floor(attackPower * damageMultiplier * damageVariance + 0.5f));
        float hitChance = Clamp01(hitRate - targetDodgeRate + attackerAccuracyRate);
        bool isHit = hitRoll <= hitChance;
        bool appliesEffect = isHit && damage > 0 && effectRoll <= Clamp01(effectRate);

        return new CombatAttackResult(damage, isHit, appliesEffect);
    }

    private static float Clamp01(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }
    public static float CalculateEscapeProbability(Character player, Character enemy)
    {
        if (player == null || enemy == null) return 0f;
        return CalculateEscapeProbability(player.lv, enemy.lv,
            player.CurrentHP, player.MaxHP, enemy.CurrentHP, enemy.MaxHP);
    }

    // 수치 계산을 분리해 씬이나 Unity 입력 없이 경계값을 검사한다.
    public static float CalculateEscapeProbability(int playerLevel, int enemyLevel,
        int playerHP, int playerMaxHP, int enemyHP, int enemyMaxHP)
    {
        if (playerMaxHP <= 0 || enemyMaxHP <= 0 || playerHP <= 0 || enemyHP <= 0) return 0f;
        long levelDifference = (long)Math.Max(1, playerLevel) - Math.Max(1, enemyLevel);
        double playerHealthRatio = (double)Math.Min(playerHP, playerMaxHP) / playerMaxHP;
        double enemyHealthRatio = (double)Math.Min(enemyHP, enemyMaxHP) / enemyMaxHP;
        double probability = 0.3d + 0.05d * levelDifference + 0.5d * (playerHealthRatio - enemyHealthRatio);
        return (float)Math.Max(0d, Math.Min(1d, probability));
    }
}

public struct CombatAttackResult
{
    public int Damage { get; }
    public bool IsHit { get; }
    public bool AppliesEffect { get; }

    public CombatAttackResult(int damage, bool isHit, bool appliesEffect)
    {
        Damage = damage;
        IsHit = isHit;
        AppliesEffect = appliesEffect;
    }
}
