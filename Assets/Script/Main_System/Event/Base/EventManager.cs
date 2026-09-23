using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : SingleTon<EventManager>
{
    private bool processing;
    public RewardResult Execute(EventNode node, EventOption option)
    {
        RewardResult Fail(string message) => new RewardResult { message = message };
        if (processing) return Fail("처리 중입니다.");
        if (node == null || node.definition == null || option == null || !node.definition.options.Contains(option)) return Fail("유효하지 않은 선택입니다.");
        if (NodeManager.Instance == null || NodeManager.Instance.currentNode != node) return Fail("현재 이벤트가 아닙니다.");
        string definitionError = EventDefinitionValidator.Validate(node.definition);
        if (definitionError != null) return Fail(definitionError);
        var game = GameManager.Instance;
        var player = CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null;
        if (game == null || player == null) return Fail("플레이어 정보를 찾지 못했습니다.");
        var progress = game.eventProgress;
        string key = node.name + "/" + option.id;
        if (!option.repeatable && progress.claimed.Contains(key)) return Fail("이미 처리한 선택입니다.");
        if (!string.IsNullOrEmpty(option.requiredQuest) && !progress.acceptedQuests.Contains(option.requiredQuest)) return Fail("먼저 퀘스트를 수락해야 합니다.");
        if (option.action == EventActionKind.AcceptQuest && progress.acceptedQuests.Contains(option.questId)) return Fail("이미 수락한 퀘스트입니다.");
        if (option.action == EventActionKind.CompleteQuest && (!progress.acceptedQuests.Contains(option.questId) || progress.completedQuests.Contains(option.questId))) return Fail("완료할 수 없는 퀘스트입니다.");
        var inventory = InventoryManager.Instance;
        if (option.requiredItem != null)
        {
            long count = 0;
            if (inventory != null) foreach (var item in inventory.inventoryItems)
                if (item != null && item.itemID == option.requiredItem.itemID) count += item is ConsumableItem stack ? stack.quantity : 1;
            if (count < option.requiredQuantity) return Fail("필요한 아이템 수량이 부족합니다.");
        }
        processing = true;
        try
        {
            var result = RewardService.Apply(option.reward, player, inventory, CurrencyManager.Instance, option.goldCost, option.goldLoss);
            if (!result.success) return result;
            if (!option.repeatable) progress.claimed.Add(key);
            if (option.action == EventActionKind.AcceptQuest) progress.acceptedQuests.Add(option.questId);
            if (option.action == EventActionKind.CompleteQuest) progress.completedQuests.Add(option.questId);
            GameEvent.SaveGame();
            return result;
        }
        finally { processing = false; }
    }

    public void Choose(Choice choice)
    {
        if (choice == null || choice.baseEvent == null) return;
        Debug.Log($"{choice.baseEvent} : Event 실행.");
        choice.baseEvent.Execute(choice.nextNode);
    }
}
