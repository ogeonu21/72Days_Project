
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RewardSystem : SingleTon<RewardSystem>
{
    public void OnEnable()
    {
        RewardEvent.OnRewardProcess += ProcessReward;
    }
    public void OnDisable()
    {
        RewardEvent.OnRewardProcess -= ProcessReward;
    }

    private IEnumerator ProcessReward(Enemy enemy, Player player)
    {
        yield return GameEvent.OnNodeTextUpdate($"당신은 보상으로 {enemy.GetExpReward()}의 경험치를 획득하였다.");
        yield return StartCoroutine(WaitForClick.WaitClick());

        player.GetExp(enemy.GetExpReward());

        if(CalculateFunction.Roll(0.2f))
        {


            
            yield return GameEvent.OnNodeTextUpdate($"당신은 보상으로 ....을 얻을 예정이다.");
            yield return StartCoroutine(WaitForClick.WaitClick());

        }
        yield return null;

    }
}
