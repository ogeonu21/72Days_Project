using System;
using System.Collections;

public static class CombatEvent
{
//캐릭터 UI 변경 이벤트
    public static event Action<Player, Enemy> OnCharacterUIChanged;
    public static void UpdateCharacterUI(Player player, Enemy enemy) => OnCharacterUIChanged?.Invoke(player, enemy);

}
