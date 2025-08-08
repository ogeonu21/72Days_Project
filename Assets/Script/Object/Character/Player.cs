using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Player : Character
{
    private void Start()
    {
        //HP바 연동 필요.
        status.OnHPChanged += UpdateUI;
    }

    private void UpdateUI(float currentHP)
    {
        //HP바 갱신.
        //플레이어 피격 이펙트 출력.
    }

    protected override void Die()
    {
        base.Die();

        //플레이어 전용 사망처리.
    }
}
