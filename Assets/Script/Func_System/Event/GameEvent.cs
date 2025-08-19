using System;

public static class GameEvent
{
    public static event Action<int, int> OnTakeDamageEffect;
    public static void OnTakeDamage(int currentHP, int maxHP) => OnTakeDamageEffect?.Invoke(currentHP, maxHP);
}
