
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RewardManager : SingleTon<RewardManager>
{
    Player player;
    
    public void OnEnable()
    {
        RewardEvent.OnRewardProcess += ProcessReward;
    }
    public void OnDisable()
    {
        RewardEvent.OnRewardProcess -= ProcessReward;
    }

    private IEnumerator ProcessReward(Reward reward)
    {
        player = CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null;
        string error = RewardService.Validate(reward);
        if (error != null || player == null) { Debug.LogError(error ?? "보상 대상이 없습니다."); yield break; }
        // 전투는 기존 정책 유지: 가방이 가득 차도 경험치/골드는 지급한다.
        var entries = RewardService.Entries(reward);
        reward.dropItem = null; reward.itemDropRate = 0; reward.items = null;
        var result = RewardService.Apply(reward, player, InventoryManager.Instance, CurrencyManager.Instance);
        yield return GameEvent.OnNodeTextUpdate(result.message);
        yield return WaitForClick.WaitClick();
        foreach (var entry in entries)
        {
            if (entry.probability <= 0 || (entry.probability < 1 && UnityEngine.Random.value >= entry.probability)) continue;
            var itemReward = new Reward { items = new List<ItemReward> { new ItemReward { item = entry.item, quantity = entry.quantity, probability = 1 } } };
            var itemResult = RewardService.Apply(itemReward, player, InventoryManager.Instance, CurrencyManager.Instance);
            yield return GameEvent.OnNodeTextUpdate(itemResult.message);
            yield return WaitForClick.WaitClick();
        }

    }
}
