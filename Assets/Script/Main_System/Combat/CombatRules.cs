using System;

/// <summary>
/// Unity 생명주기와 분리된 전투 판정 규칙입니다.
/// 난수는 호출자가 주입하므로 동일 입력에 대해 항상 같은 결과를 만듭니다.
/// </summary>
public static class CombatRules
{
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
