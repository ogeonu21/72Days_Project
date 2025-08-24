using System;

public static class GameEvent
{
    public static event Action<int, int> OnTakeDamageEffect;
    public static void OnTakeDamage(int currentHP, int maxHP) => OnTakeDamageEffect?.Invoke(currentHP, maxHP);

    public static event Action<Node> OnNodeChanged;
    public static void NotifyNodeChange(Node node) => OnNodeChanged?.Invoke(node);

    public static event Action<Player, Enemy> OnCharacterUIChanged;
    public static void UpdateCharacterUI(Player player, Enemy enemy) => OnCharacterUIChanged?.Invoke(player, enemy);
}
