using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{

    public void ReLoadObject()
    {
        LoadName();
        LoadStatus(name); //특정 캐릭터 이름의 스테이터스 정보를 불러옴
        if (nameText != null)
        {
            nameText.text = name;
            _hpBar.value = (float)hp / maxHP;
            _hpText.text = hp.ToString() + "/" + maxHP.ToString();
        }
        originalAttack = attack;
        originalAttackDistance = attackDistance;
    }

}
