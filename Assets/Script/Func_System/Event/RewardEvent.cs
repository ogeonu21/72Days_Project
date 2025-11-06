using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RewardEvent
{
    public delegate IEnumerator RewardProcess(Enemy enemy, Player player);
    public static event RewardProcess OnRewardProcess;
    public static IEnumerator RewardCoroutine(Enemy enemy, Player player)
    {
        yield return OnRewardProcess?.Invoke(enemy, player);
    }
}
