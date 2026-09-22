
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.Build.Pipeline;

public class RewardManager : SingleTon<RewardManager>
{
    Player player;
    
    public void OnEnable()
    {
        RewardEvent.OnRewardProcess += ProcessReward;
        player = CharacterManager.Instance.playerPrefab;
    }
    public void OnDisable()
    {
        RewardEvent.OnRewardProcess -= ProcessReward;
    }

    private IEnumerator ProcessReward(Reward reward)
    {
        this.player = CharacterManager.Instance.currentPlayer != null ? CharacterManager.Instance.currentPlayer : null;
        if(this.player == null)
        {
            Debug.Log("[RewardManager.cs] 에러 발생. player 특정 불가.");
            yield return null;
        }

        yield return GameEvent.OnNodeTextUpdate($"당신은 보상으로 {reward.exp}의 경험치를 획득하였다.");
        yield return StartCoroutine(WaitForClick.WaitClick());

        player.GetExp(reward.exp);
        CurrencyManager.Instance.Increase("Gold", reward.dropGold);

        if(CalculateFunction.Roll(reward.itemDropRate))
        {
            if (!InventoryManager.Instance.CanAdd(reward.dropItem))
            {
                yield return GameEvent.OnNodeTextUpdate($"배낭이 꽉 차 더 이상 아이템을 얻을 수 없다.");
            }
            else{
                yield return GameEvent.OnNodeTextUpdate($"당신은 보상으로 {reward.dropItem.itemName}을 얻었다.");
                InventoryManager.Instance.AddToInventory(reward.dropItem);
            }
            yield return StartCoroutine(WaitForClick.WaitClick());

        }
        yield return null;

    }
}
