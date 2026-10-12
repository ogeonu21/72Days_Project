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
        //이게 결국 조건을 확인하는 것.
        return EventChoiceEvaluator.Check(option, node.name + "/" + option.id,
            CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null,
            InventoryManager.Instance, CurrencyManager.Instance, GameManager.Instance != null ? GameManager.Instance.eventProgress : null);
    }

    //얘가 실행하는거야.
    //굳이 반환을 해야하는가?
    public RewardResult Execute(EventNode node, EventOption option, System.Func<float> roll = null)
    {
        RewardResult Fail(string message) => new RewardResult { message = message };
        //여기서 실행가능 여부를 한 번 더 체크하네? 중복이긴하다.
        string reason = GetUnavailableReason(node, option);
        if (reason != null) return Fail(reason);

        //얘는 왜 이 인스턴스를 한 번 더 확인할까?
        var game = GameManager.Instance;
        var player = CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null;
        if (game == null || player == null) return Fail("플레이어 정보를 찾지 못했습니다.");

        //gameManager의 eventProgress를 복사
        var progress = game.eventProgress;
        var inventory = InventoryManager.Instance;
        processing = true;
        try
        {
            var result = ApplyOption(node, option, player, inventory, progress, roll);
            if (result.success && option.action == EventActionKind.AcceptQuest)
            {
                // processing 잠금은 유지하되, 표시와 동일한 판정기로 완료 조건을 다시 검사한다.
                foreach (var completion in node.definition.options)
                {
                    if (completion.action != EventActionKind.CompleteQuest || completion.questId != option.questId) continue;
                    string unavailable = EventChoiceEvaluator.Check(completion, node.name + "/" + completion.id,
                        player, inventory, CurrencyManager.Instance, progress);
                    if (unavailable != null) continue;
                    var completed = ApplyOption(node, completion, player, inventory, progress, roll);
                    result.message += "\n" + completed.message;
                    if (completed.success)
                    {
                        result.completedQuestOption = completion;
                        result.healed += completed.healed;
                        result.grantedItems.AddRange(completed.grantedItems);
                    }
                    // 한 번의 수락에서 하나의 완료 처리만 시도한다.
                    break;
                }
            }
            if (result.success || result.chanceFailed) GameEvent.SaveGame();
            return result;
        }
        finally { processing = false; ProgressChanged?.Invoke(); }
    }
    private RewardResult ApplyOption(EventNode node, EventOption option, Player player,
        InventoryManager inventory, EventProgress progress, System.Func<float> roll)
    {
        string key = node.name + "/" + option.id;
        if (!option.RollSuccess(roll))
        {
            if (!option.repeatable) progress.claimed.Add(key);
            return new RewardResult { chanceFailed = true, message = "시도에 실패하였습니다." };
        }
        var result = RewardService.Apply(option.reward, player, inventory, CurrencyManager.Instance, option.goldCost, option.goldLoss);
        if (!result.success) return result;
        if (!option.repeatable) progress.claimed.Add(key);
        if (option.action == EventActionKind.AcceptQuest)
        {
            progress.acceptedQuests.Add(option.questId);
            result.message += "\n퀘스트 수락: " + option.questId;
        }
        if (option.action == EventActionKind.CompleteQuest)
        {
            progress.completedQuests.Add(option.questId);
            result.message += "\n퀘스트 완료: " + option.questId;
        }
        return result;
    }

}
