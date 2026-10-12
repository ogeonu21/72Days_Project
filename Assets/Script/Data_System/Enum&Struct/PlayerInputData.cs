///<summary> 플레이어의 행동을 정의하는 enum 데이터 </summary>///

public enum PlayerBehaviour
{
    Attack, //플레이어가 공격 행동을 취함.
    Use,    //플레이어가 아이템 사용 행동을 취함.
    Run     //플레이어가 도망 행동을 취함.
}

public enum AttackArea
{
    Head = 0,
    Torso = 1,
    Arm = 2,
    Leg = 3
}

[System.Serializable]
public struct PlayerInputData
{
    public PlayerBehaviour playerBehaviour;
    public AreaData areaData;
    public BaseItem baseItem;

    public PlayerInputData(PlayerBehaviour playerBehaviour, AreaData areaData, BaseItem baseItem)
    {
        this.playerBehaviour = playerBehaviour;
        this.areaData = areaData;
        this.baseItem = baseItem;
    }
}