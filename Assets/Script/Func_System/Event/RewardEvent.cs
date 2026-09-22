using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RewardEvent
{
    public delegate IEnumerator RewardProcess(Reward reward);
    public static event RewardProcess OnRewardProcess;
    public static IEnumerator RewardCoroutine(Reward reward)
    {
        yield return OnRewardProcess?.Invoke(reward);
    }
}
