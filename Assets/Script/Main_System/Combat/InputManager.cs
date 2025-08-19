using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InputManager : MonoBehaviour
{
    private CombatManager combatManager;
    
    private void Awake()
    {
        combatManager = CombatManager.Instance;
    }


    public void AttackAreaInput(string name)
    {
        if (!combatManager.combatActive) {
            return;
        }

        if (combatManager.onAttackTurn)
        {
            AreaData data = AreaDataDB.GetArea(name, out data);
            combatManager.GetInput(data);
        }
        else
        {
            Debug.Log("아직 당신의 턴이 아닙니다.");
        }   
    }
}
