using System;
using UnityEngine;

[System.Serializable]
public class Status
{
    [SerializeField] private float hp;
    [SerializeField] private float maxHP;
    [SerializeField] private float attack;
    [SerializeField] private float avoid;
    [SerializeField] private int attackDistance;

    // 원본 값 저장
    [NonSerialized] public float originalAvoid;
    [NonSerialized] public int originalAttackDistance;

    // 디버프 상태
    public int avoidDebuffTurns;
    public int attackDistanceDebuffTurns;

    // 이벤트 (옵저버 패턴용)
    public event Action<float> OnHPChanged;

    #region 읽기 전용 프로퍼티
    public float HP => hp;
    public float MaxHP => maxHP;
    public float Attack => attack;
    public float Avoid => avoid;
    public int AttackDistance => attackDistance;
    #endregion

    #region 초기화
    public Status() { }

    public Status(float hp, float maxHP, float attack, float avoid, int attackDistance)
    {
        Initialize(hp, maxHP, attack, avoid, attackDistance);
    }

    public void Initialize(float hp, float maxHP, float attack, float avoid, int attackDistance)
    {
        this.hp = hp;
        this.maxHP = maxHP;
        this.attack = attack;
        this.avoid = avoid;
        this.attackDistance = attackDistance;

        originalAvoid = avoid;
        originalAttackDistance = attackDistance;

        avoidDebuffTurns = 0;
        attackDistanceDebuffTurns = 0;
    }
    #endregion

    #region 변화 처리
    public void ModifyHP(float delta)
    {
        SetHP(hp + delta);
    }

    private void SetHP(float value)
    {
        hp = Mathf.Clamp(value, 0, maxHP);
        OnHPChanged?.Invoke(hp);
    }

    public void ModifyAttack(float delta)
    {
        attack += delta;
    }

    public void ModifyAvoid(float delta)
    {
        avoid += delta;
    }

    public void ModifyAttackDistance(int delta)
    {
        attackDistance += delta;
    }

    public void ResetAvoid()
    {
        avoid = originalAvoid;
    }

    public void ResetAttackDistance()
    {
        attackDistance = originalAttackDistance;
    }
    #endregion
}
