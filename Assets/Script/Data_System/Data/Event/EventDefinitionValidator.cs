using System;
using System.Collections.Generic;

public static class EventDefinitionValidator
{
    public static string Validate(EventDefinition definition)
    {
        if (definition == null || !Enum.IsDefined(typeof(EventKind), definition.kind)) return "이벤트 유형 오류";
        if (definition.options == null || definition.options.Count == 0) return "선택지가 필요합니다.";
        if (definition.exitNode == null) return "재방문 시에도 떠날 수 있도록 Exit Node를 지정하세요.";
        var ids = new HashSet<string>();
        foreach (var option in definition.options)
        {
            if (option == null || string.IsNullOrWhiteSpace(option.id) || !ids.Add(option.id) || string.IsNullOrWhiteSpace(option.text)) return "선택지 ID/문구 누락 또는 ID 중복";
            if (!Enum.IsDefined(typeof(EventActionKind), option.action) || option.goldCost < 0 || option.goldLoss < 0) return "동작/비용/손실 오류";
            if (option.requiredItem != null && option.requiredQuantity < 1) return "조건 아이템 수량 오류";
            if (!Enum.IsDefined(typeof(QuestRequirementState), option.requiredQuestState) ||
                !Enum.IsDefined(typeof(TendencyRequirement), option.tendencyCondition)) return "선택지 조건 유형 오류";
            if (option.requiredGold < 0 || option.requiredStats.str < 0 || option.requiredStats.dex < 0 || option.requiredStats.con < 0) return "골드/스탯 조건은 0 이상이어야 합니다.";
            if (option.tendencyCondition == TendencyRequirement.Between && option.tendencyMin > option.tendencyMax) return "성향 최소값이 최대값보다 큽니다.";
            if ((option.action == EventActionKind.AcceptQuest || option.action == EventActionKind.CompleteQuest) && (string.IsNullOrWhiteSpace(option.questId) || option.repeatable)) return "퀘스트 ID 필수, 반복 불가";
            if (option.action == EventActionKind.Combat && !(option.nextNode is CombatNode)) return "전투 이동에는 CombatNode가 필요합니다.";
            var error = RewardService.Validate(option.reward);
            if (error != null) return error;
        }
        return null;
    }
}
