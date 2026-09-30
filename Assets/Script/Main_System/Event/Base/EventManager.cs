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
        //이 key는 뭐지?
        string key = node.name + "/" + option.id;
        var inventory = InventoryManager.Instance;
        processing = true;
        try
        {
            if (!option.RollSuccess(roll))
            {
                if (!option.repeatable) progress.claimed.Add(key);
                GameEvent.SaveGame();
                return new RewardResult { chanceFailed = true, message = "시도에 실패하였습니다." };
            }
            var result = RewardService.Apply(option.reward, player, inventory, CurrencyManager.Instance, option.goldCost, option.goldLoss);
            if (!result.success) return result;
            // 반복 가능이 꺼졌다. 즉, 한번만 실행이 가능하다 이건가?
            if (!option.repeatable) progress.claimed.Add(key);
            if (option.action == EventActionKind.AcceptQuest){
                progress.acceptedQuests.Add(option.questId);
                result.message += "\n퀘스트 수락: " + option.questId;
            }
            if (option.action == EventActionKind.CompleteQuest){
                progress.completedQuests.Add(option.questId);
                result.message += "\n퀘스트 완료: " + option.questId;
            }
            GameEvent.SaveGame();
            return result;
        }
        finally { processing = false; ProgressChanged?.Invoke(); }
    }
}
