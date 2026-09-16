using System;
using System.Collections;

public static class PlayerEvent
{
    
    //Player 레벨업 이벤트
    public static event Action<int> OnPlayerLevelUp;
    public static void PlayerLevelUp(int levelDifference) => OnPlayerLevelUp?.Invoke(levelDifference);

    //Player 스탯 변경 이벤트
    public static event Action onStatsChanged;
    public static void OnStatsChanged() => onStatsChanged?.Invoke();
}