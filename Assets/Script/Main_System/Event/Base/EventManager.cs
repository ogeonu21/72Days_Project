using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : SingleTon<EventManager>
{
    private bool processing;
    public event System.Action ProgressChanged;

    public string GetUnavailableReason(EventNode node, EventOption option)
    {
        if (processing) return "처리 중입니다.";
        if (node == null || node.definition == null || option == null || !node.definition.options.Contains(option)) return "유효하지 않은 선택입니다.";
        if (NodeManager.Instance == null || NodeManager.Instance.currentNode != node) return "현재 이벤트가 아닙니다.";
        string error = EventDefinitionValidator.Validate(node.definition);
        if (error != null) return error;
        return EventChoiceEvaluator.Check(option, node.name + "/" + option.id,
            CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null,
            InventoryManager.Instance, CurrencyManager.Instance, GameManager.Instance != null ? GameManager.Instance.eventProgress : null);
    }

    public RewardResult Execute(EventNode node, EventOption option)
    {
        RewardResult Fail(string message) => new RewardResult { message = message };
        string reason = GetUnavailableReason(node, option);
        if (reason != null) return Fail(reason);
        var game = GameManager.Instance;
        var player = CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null;
        if (game == null || player == null) return Fail("플레이어 정보를 찾지 못했습니다.");
        var progress = game.eventProgress;
        string key = node.name + "/" + option.id;
        var inventory = InventoryManager.Instance;
        processing = true;
        try
        {
            var result = RewardService.Apply(option.reward, player, inventory, CurrencyManager.Instance, option.goldCost, option.goldLoss);
            if (!result.success) return result;
            if (!option.repeatable) progress.claimed.Add(key);
            if (option.action == EventActionKind.AcceptQuest) progress.acceptedQuests.Add(option.questId);
            if (option.action == EventActionKind.CompleteQuest) progress.completedQuests.Add(option.questId);
            if (option.action == EventActionKind.AcceptQuest) result.message += "\n퀘스트 수락: " + option.questId;
            if (option.action == EventActionKind.CompleteQuest) result.message += "\n퀘스트 완료: " + option.questId;
            GameEvent.SaveGame();
            return result;
        }
        finally { processing = false; ProgressChanged?.Invoke(); }
    }
}
