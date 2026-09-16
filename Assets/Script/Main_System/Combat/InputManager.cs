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


    public void AttackAreaInput(int serializedArea)
    {
        if (!combatManager.combatActive || player == null)
        {
            return;
        }

        if (!System.Enum.IsDefined(typeof(AttackArea), serializedArea))
        {
            Debug.LogWarning($"[InputManager] 알 수 없는 공격 부위 값입니다: {serializedArea}");
            return;
        }

        if (!combatManager.onAttackTurn)
        {
            return;
        }

        AttackArea area = (AttackArea)serializedArea;
        int index = (int)area;
        if (player.areaDataDB == null || index >= player.areaDataDB.Length)
        {
            Debug.LogError($"[InputManager] {area}에 대응하는 공격 부위 데이터를 찾을 수 없습니다.");
            return;
        }

        combatManager.GetInput(player.areaDataDB[index]);
    }
}
