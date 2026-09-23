using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>UI와 분리된 보상 처리. 비용과 최대 필요 공간을 검사한 뒤 한 번만 추첨한다.</summary>
public static class RewardService
{
    public static List<ItemReward> Entries(Reward reward)
    {
        var entries = new List<ItemReward>();
        if (reward.dropItem != null && reward.itemDropRate > 0)
            entries.Add(new ItemReward { item = reward.dropItem, quantity = 1, probability = reward.itemDropRate });
        if (reward.items != null) entries.AddRange(reward.items);
        return entries;
    }

    public static string Validate(Reward reward)
    {
        if (reward.exp < 0 || reward.hpHeal < 0 || reward.dropGold < 0 || reward.statIncrease.str < 0 || reward.statIncrease.dex < 0 || reward.statIncrease.con < 0)
            return "보상 수치는 음수일 수 없습니다. 비용 또는 손실을 사용하세요.";
        if (float.IsNaN(reward.itemDropRate) || reward.itemDropRate < 0 || reward.itemDropRate > 1 || (reward.itemDropRate > 0 && reward.dropItem == null))
            return "기존 드롭 아이템/확률 설정 오류";
        foreach (var entry in Entries(reward))
            if (entry == null || entry.item == null || string.IsNullOrWhiteSpace(entry.item.itemID) || entry.quantity < 1 || entry.quantity > 1000 || float.IsNaN(entry.probability) || entry.probability < 0 || entry.probability > 1)
                return "아이템, 수량(1~1000), 확률(0~1)을 확인하세요.";
        return null;
    }

    public static RewardResult Apply(Reward reward, Player player, InventoryManager inventory, CurrencyManager currency,
        int cost = 0, int loss = 0, Func<float> roll = null)
    {
        var result = new RewardResult();
        string error = Validate(reward);
        if (error != null) { result.message = error; return result; }
        if (player == null || player.IsDead || cost < 0 || loss < 0) { result.message = "현재 보상을 처리할 수 없습니다."; return result; }
        if ((long)player.baseStats.str + reward.statIncrease.str > 100000 ||
            (long)player.baseStats.dex + reward.statIncrease.dex > 100000 ||
            (long)player.baseStats.con + reward.statIncrease.con > 100000 ||
            (long)player.GetCurrentData().exp + reward.exp > int.MaxValue)
        { result.message = "능력치 또는 경험치 안전 상한을 초과합니다."; return result; }
        long nextTendency = (long)player.tendency + reward.tendencyChange;
        if (nextTendency < int.MinValue || nextTendency > int.MaxValue)
        { result.message = "성향 안전 상한을 초과합니다."; return result; }
        var gold = currency != null ? currency.GetCurrencyData("Gold") : null;
        if ((cost > 0 || loss > 0 || reward.dropGold > 0) && gold == null) { result.message = "골드 정보가 없습니다."; return result; }
        if (cost > (gold?.Amount ?? 0)) { result.message = "골드가 부족합니다."; return result; }
        long finalGold = Math.Max(0L, (long)(gold?.Amount ?? 0) - cost - loss) + reward.dropGold;
        long actualLoss = Math.Min(loss, (long)(gold?.Amount ?? 0) - cost);
        if (finalGold > int.MaxValue) { result.message = "골드 상한을 초과합니다."; return result; }
        var entries = Entries(reward);
        if (entries.Count > 0 && (inventory == null || !inventory.CanAddRewards(entries)))
        { result.message = "보상을 받을 가방 공간 또는 수량 여유가 부족합니다. 지급·결제하지 않았습니다."; return result; }
        roll = roll ?? (() => UnityEngine.Random.value);
        foreach (var entry in entries)
            if (entry.probability >= 1 || (entry.probability > 0 && roll() < entry.probability)) result.grantedItems.Add(entry);
        // 이 지점 이후에는 사용자 입력이나 프레임 대기를 끼우지 않는다.
        foreach (var entry in result.grantedItems) inventory.AddReward(entry.item, entry.quantity);
        if (gold != null && (cost > 0 || loss > 0 || reward.dropGold > 0)) gold.SetAmount((int)finalGold);
        if (reward.statIncrease.str != 0 || reward.statIncrease.dex != 0 || reward.statIncrease.con != 0)
        {
            player.baseStats.str += reward.statIncrease.str;
            player.baseStats.dex += reward.statIncrease.dex;
            player.baseStats.con += reward.statIncrease.con;
            player.UpdateStats(true);
        }
        int before = player.CurrentHP;
        if (reward.hpHeal > 0) player.Heal(reward.hpHeal);
        result.healed = player.CurrentHP - before;
        if (reward.exp > 0) player.GetExp(reward.exp);
        if (reward.tendencyChange != 0) player.ChangeTendency(reward.tendencyChange);
        var text = new StringBuilder();
        if (cost > 0) text.AppendLine($"비용: {cost} 골드");
        if (loss > 0) text.AppendLine($"골드 손실: {actualLoss}");
        if (reward.dropGold > 0) text.AppendLine($"골드 +{reward.dropGold}");
        if (reward.exp > 0) text.AppendLine($"경험치 +{reward.exp}");
        if (reward.hpHeal > 0) text.AppendLine($"체력 {result.healed} 회복");
        if (reward.statIncrease.str > 0) text.AppendLine($"STR +{reward.statIncrease.str}");
        if (reward.statIncrease.dex > 0) text.AppendLine($"DEX +{reward.statIncrease.dex}");
        if (reward.statIncrease.con > 0) text.AppendLine($"CON +{reward.statIncrease.con}");
        if (reward.tendencyChange != 0) text.AppendLine($"성향 {reward.tendencyChange:+0;-0;0}");
        foreach (var entry in result.grantedItems) text.AppendLine($"{entry.item.itemName} × {entry.quantity}");
        result.success = true;
        result.message = text.Length > 0 ? text.ToString().TrimEnd() : "처리되었습니다.";
        return result;
    }
}
