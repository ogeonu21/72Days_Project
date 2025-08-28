using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InputManager : MonoBehaviour
{
    private CombatManager combatManager;
    private Player player;
    
    private void Awake()
    {
        combatManager = CombatManager.Instance;
    }

    private void OnEnable()
    {
        if (CharacterManager.Instance.currentPlayer != null)
        {
            player = CharacterManager.Instance.currentPlayer;
        }
    }
    private void OnDisable()
    {
        player = null;
    }


    public void AttackAreaInput(string name)
    {
        if (!combatManager.combatActive) {
            return;
        }

        if (combatManager.onAttackTurn)
        {
            AreaData data = new AreaData();
            switch (name)
            {
                case "머리":
                    data = player.areaDataDB[0];
                    break;
                case "몸":
                    data = player.areaDataDB[1];
                    break;
                case "팔":
                    data = player.areaDataDB[2];
                    break;
                case "다리":
                    data = player.areaDataDB[3];
                    break;
                default:
                    break;
            }
            combatManager.GetInput(data);
        }
        else
        {
            Debug.Log("아직 당신의 턴이 아닙니다.");
        }   
    }
}
