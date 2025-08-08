using UnityEngine;

public class Enemy : Character
{
    private void Start()
    {
        //HP바 연동 필요.
        status.OnHPChanged += UpdateUI;
    }

    private void UpdateUI(float currentHP)
    {
        //HP바 갱신
        //적 피격 이펙트, 진동 흔들림, 붉게 변함 등등.
    }

    protected override void Die()
    {
        base.Die();

        // 적 전용 사망 처리. 사망 대사 등등 출력.
    }
}
