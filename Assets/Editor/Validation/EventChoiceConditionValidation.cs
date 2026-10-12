using System;
using UnityEditor;
using UnityEngine;

public static class EventChoiceConditionValidation
{
    [MenuItem("Tools/Validation/Event Choice Conditions")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode 전용 검사입니다.");
        int count = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new InvalidOperationException(label); count++; };
        var root = new GameObject("ConditionValidation") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        var item = ScriptableObject.CreateInstance<PotionItem>();
        var definition = ScriptableObject.CreateInstance<EventDefinition>();
        var exit = ScriptableObject.CreateInstance<EndingNode>();
        try
        {
            var player = root.AddComponent<Player>(); player.InitializeFromData(new PlayerData());
            var inventory = root.AddComponent<InventoryManager>();
            var currency = root.AddComponent<CurrencyManager>(); currency.currencyList.Add(new CurrencyData("Gold", 50));
            item.itemID = "ConditionPotion"; item.itemName = "검사 물약";
            var state = new EventProgress();
            var option = new EventOption { id = "test", text = "검사" };
            Func<bool> enabled = () => EventChoiceEvaluator.Check(option, "Node/test", player, inventory, currency, state) == null;
            check(enabled(), "무조건 선택지");
            option.requiredGold = 51; check(!enabled(), "보유 골드 부족");
            option.requiredGold = 50; check(enabled() && currency.GetAmount("Gold") == 50, "보유 골드 경계 및 판정 무소비");
            option.goldCost = 51; check(!enabled(), "비용 독립 검사"); option.goldCost = 0;
            option.requiredItem = item; option.requiredQuantity = 2;
            inventory.AddReward(item, 1); check(!enabled(), "아이템 수량 부족");
            inventory.AddReward(item, 1); check(enabled(), "ID 기준 스택 합산 경계");
            option.requiredStats = new BaseStats(1, 1, 1); check(!enabled(), "스탯 부족");
            player.baseStats = new BaseStats(1, 1, 0); player.UpdateStats(); check(!enabled(), "스탯 조건 AND");
            player.baseStats.con = 1; player.UpdateStats(); check(enabled(), "스탯 경계 충족");
            option.tendencyCondition = TendencyRequirement.AtLeast; option.tendencyMin = 1; check(!enabled(), "성향 하한 미달");
            player.ChangeTendency(1); check(enabled(), "성향 하한 포함");
            option.tendencyCondition = TendencyRequirement.AtMost; option.tendencyMax = -1; check(!enabled(), "성향 상한 초과");
            player.ChangeTendency(-2); check(enabled(), "음수 성향 상한 포함");
            option.tendencyCondition = TendencyRequirement.Between; option.tendencyMin = -1; option.tendencyMax = 1;
            check(enabled(), "성향 구간 하한 포함"); player.ChangeTendency(2); check(enabled(), "성향 구간 상한 포함");
            player.ChangeTendency(1); check(!enabled(), "성향 구간 초과"); option.tendencyCondition = TendencyRequirement.Any;
            option.requiredQuest = "Q"; check(!enabled(), "퀘스트 미수락 잠김");
            state.acceptedQuests.Add("Q"); check(enabled(), "진행 중 퀘스트 해금");
            state.completedQuests.Add("Q"); check(!enabled(), "완료는 진행 중 아님");
            option.requiredQuestState = QuestRequirementState.Completed; check(enabled(), "완료 조건 해금");
            option.requiredQuestState = QuestRequirementState.NotAccepted; check(!enabled(), "미수락 조건 차단");
            option.requiredQuestState = QuestRequirementState.Accepted; check(enabled(), "완료 후 수락 이력 유지");
            option.requiredQuest = "";
            option.action = EventActionKind.AcceptQuest; option.questId = "Q"; check(!enabled(), "중복 수락 차단");
            option.action = EventActionKind.CompleteQuest; check(!enabled(), "중복 완료 차단");
            state.completedQuests.Clear(); check(enabled(), "진행 중 퀘스트 완료 허용");
            state.acceptedQuests.Clear(); check(!enabled(), "수락 전 완료 차단");
            option.action = EventActionKind.None;
            state.claimed.Add("Node/test"); check(!enabled(), "단발 보상 재수령 차단");
            option.repeatable = true; check(enabled(), "반복 선택 허용");
            option.requiredGold = 51; check(!enabled(), "반복이어도 조건 재검사"); option.requiredGold = 0;
            var result = RewardService.Apply(new Reward { tendencyChange = -3 }, player, inventory, currency);
            check(result.success && player.tendency == -1, "성향 감소 보상");
            result = RewardService.Apply(new Reward { tendencyChange = 2 }, player, inventory, currency);
            check(result.success && player.tendency == 1, "성향 증가 보상");
            var save = new SaveGameData { player = PlayerSaveData.FromPlayerData(player.GetCurrentData()), eventProgress = state };
            state.acceptedQuests.Add("Q"); state.completedQuests.Add("Q");
            check(SaveGameData.TryDeserialize(JsonUtility.ToJson(save), out var restored, out _) && restored.eventProgress.completedQuests.Contains("Q") && restored.eventProgress.claimed.Contains("Node/test"), "퀘스트 완료·수령 상태 저장 왕복");
            definition.exitNode = exit; definition.options.Add(option);
            option.tendencyCondition = TendencyRequirement.Between; option.tendencyMin = 2; option.tendencyMax = 1;
            check(EventDefinitionValidator.Validate(definition) != null, "역전 성향 범위 거부");
            option.tendencyCondition = TendencyRequirement.Any; option.requiredGold = -1;
            check(EventDefinitionValidator.Validate(definition) != null, "음수 골드 조건 거부");
            return $"Event Choice Conditions {count}개 통과 (임시 객체, 실제 세이브 변경 없음)";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(item);
            UnityEngine.Object.DestroyImmediate(definition); UnityEngine.Object.DestroyImmediate(exit);
        }
    }
}
