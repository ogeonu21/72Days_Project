using System;
using System.Collections;

public static class PlayerEvent
{
    
    //Player 레벨업 이벤트
    public static event Action OnPlayerLevelUp;
    public static void PlayerLevelUp() => OnPlayerLevelUp?.Invoke();
}