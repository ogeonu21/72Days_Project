using System;
using System.Collections.Generic;

/// <summary>표시와 실행이 함께 사용하는 읽기 전용 조건 판정. 조건은 모두 AND이다.</summary>
public static class EventChoiceEvaluator
{
    public static string Check(EventOption option, string claimKey, Player player,
        InventoryManager inventory, CurrencyManager currency, EventProgress progress)
    {
        if (option == null || player == null || progress == null) return "플레이어 정보를 불러오는 중입니다.";
        if (player.IsDead) return "사망한 상태에서는 선택할 수 없습니다.";
        if (!option.repeatable && progress.claimed.Contains(claimKey)) return "이미 처리한 선택입니다.";
        var reasons = new List<string>();
        bool accepted = progress.acceptedQuests.Contains(option.requiredQuest);
        bool completed = progress.completedQuests.Contains(option.requiredQuest);
        if (!string.IsNullOrEmpty(option.requiredQuest))
        {
            bool matches;
            switch (option.requiredQuestState)
            {
                case QuestRequirementState.Active: matches = accepted && !completed; break;
                case QuestRequirementState.Completed: matches = completed; break;
                case QuestRequirementState.NotAccepted: matches = !accepted && !completed; break;
                case QuestRequirementState.Accepted: matches = accepted || completed; break;
                default: matches = false; break;
            }
            if (!matches) reasons.Add($"퀘스트 {option.requiredQuest}: {QuestLabel(option.requiredQuestState)} 필요");
        }
        if (option.action == EventActionKind.AcceptQuest &&
            (progress.acceptedQuests.Contains(option.questId) || progress.completedQuests.Contains(option.questId)))
            reasons.Add("이미 수락한 퀘스트입니다.");
        if (option.action == EventActionKind.CompleteQuest &&
            (!progress.acceptedQuests.Contains(option.questId) || progress.completedQuests.Contains(option.questId)))
            reasons.Add("진행 중인 퀘스트만 완료할 수 있습니다.");
        if (option.tendencyCondition == TendencyRequirement.AtLeast || option.tendencyCondition == TendencyRequirement.Between)
            if (player.tendency < option.tendencyMin) reasons.Add($"성향 {option.tendencyMin} 이상 필요 (현재 {player.tendency})");
        if (option.tendencyCondition == TendencyRequirement.AtMost || option.tendencyCondition == TendencyRequirement.Between)
            if (player.tendency > option.tendencyMax) reasons.Add($"성향 {option.tendencyMax} 이하 필요 (현재 {player.tendency})");
        int goldNeeded = Math.Max(option.requiredGold, option.goldCost);
        int gold = currency != null ? currency.GetCurrencyData("Gold")?.Amount ?? 0 : 0;
        if (gold < goldNeeded) reasons.Add($"골드 {goldNeeded} 필요 (현재 {gold})");
        if (option.requiredItem != null)
        {
            long count = 0;
            if (inventory != null) foreach (var item in inventory.inventoryItems)
                if (item != null && item.itemID == option.requiredItem.itemID)
                    count += item is ConsumableItem stack ? Math.Max(0, stack.quantity) : 1;
            if (count < option.requiredQuantity) reasons.Add($"{option.requiredItem.itemName} {option.requiredQuantity}개 필요 (현재 {count})");
        }
        if (player.baseStats.str < option.requiredStats.str) reasons.Add($"STR {option.requiredStats.str} 필요 (현재 {player.baseStats.str})");
        if (player.baseStats.dex < option.requiredStats.dex) reasons.Add($"DEX {option.requiredStats.dex} 필요 (현재 {player.baseStats.dex})");
        if (player.baseStats.con < option.requiredStats.con) reasons.Add($"CON {option.requiredStats.con} 필요 (현재 {player.baseStats.con})");
        return reasons.Count == 0 ? null : string.Join("\n", reasons);
    }

    private static string QuestLabel(QuestRequirementState state)
    {
        switch (state)
        {
            case QuestRequirementState.Active: return "진행 중";
            case QuestRequirementState.Completed: return "완료";
            case QuestRequirementState.NotAccepted: return "미수락";
            default: return "수락 이력";
        }
    }
}
