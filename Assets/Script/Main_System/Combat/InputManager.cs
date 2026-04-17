using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InputManager : MonoBehaviour
{
    #region [변수 그룹]
    //변수 목록.
    private CombatManager combatManager;
    private Player player;
    #endregion

    #region [초기화]
    //배틀 매니저 instance 연결.
    private void Awake()
    {
        combatManager = CombatManager.Instance;
    }

    //InputManager가 활성화될 경우, player를 instance를 받아옴.
    private void OnEnable()
    {
        if (CharacterManager.Instance.currentPlayer != null)
        {
            player = CharacterManager.Instance.currentPlayer;
        }
    }

    //비활성화시, 연결 해제.
    private void OnDisable()
    {
        player = null;
    }
    #endregion


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
            //배틀 매니저와 연결. 이거 이벤트로 바꿀 수 있나?
            combatManager.GetInput(data);
        }   
    }
}
