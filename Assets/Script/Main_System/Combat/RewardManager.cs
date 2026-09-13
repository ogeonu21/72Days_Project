
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RewardManager : SingleTon<RewardManager>
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
        CurrencyManager.Instance.Increase("Gold", enemy.dropGold);

        if(CalculateFunction.Roll(
            enemy.itemDropRate))
        {
            yield return GameEvent.OnNodeTextUpdate($"당신은 보상으로 {enemy.dropItem.itemName}을 얻었다.");
            InventoryManager.Instance.AddToInventory(enemy.dropItem);
            yield return StartCoroutine(WaitForClick.WaitClick());

        }
        yield return null;

    }
}
