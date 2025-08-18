using System;
using UnityEngine;

public class CharacterManager : SingleTon<CharacterManager>
{
    #region [변수 그룹]
    [Header("프리팹")]
    public Player playerPrefab;
    public Enemy enemyPrefab;

    public Player currentPlayer { get; private set; }
    public Enemy currentEnemy { get; private set; }

    //인스펙터를 통해 할당받는 것이 아닌, 코드로 등록받을 변수.
    private Transform registeredPlayerParent;
    private Transform registeredEnemyParent;
    #endregion

    #region [이벤트 그룹]
    public Action<Player, Enemy> OnCharacterReady;
    #endregion

    #region [객체 생성]
    public void RegisterPlayerParent(Transform parent)
    {
        registeredPlayerParent = parent;
        Debug.Log("플레이어 생성 위치 등록 완료");
    }

    public void RegisterEnemyParent(Transform parent)
    {
        registeredEnemyParent = parent;
        Debug.Log("적 생성 위치 등록 완료");
    }

    public void SpawnCharacter(PlayerData data, int index)
    {
        //Player 생성.
        #region initialize
        if (registeredPlayerParent == null)
        {
            Debug.LogError("플레이어 생성 위치 UnFound.");
            return;
        }

        if (currentPlayer == null)
        {
            currentPlayer = Instantiate(playerPrefab, registeredPlayerParent);
        }
        #endregion

        if (index == 0)
        {
            currentPlayer.InitializeFromData(data);
        }
        else if (index == 1)
        {
            currentPlayer.LoadFromData(data);
        }
        else
        {
            currentPlayer.InitializeFromData(data);
            Debug.Log("플레이어 생성 모드 index 오류! 0:New 혹은 1:Load로 설정하시오.");
        }

        currentPlayer.gameObject.SetActive(true);


        //Enemy 생성.
        if (registeredEnemyParent == null)
        {
            Debug.LogError("적 생성 위치 UnFound.");
            return;
        }

        if (currentEnemy == null)
        {
            currentEnemy = Instantiate(enemyPrefab, registeredEnemyParent);

        }

        currentEnemy.gameObject.SetActive(false);

        OnCharacterReady?.Invoke(currentPlayer, currentEnemy);
    }
    #endregion

}
