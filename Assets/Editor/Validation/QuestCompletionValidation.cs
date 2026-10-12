using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class QuestCompletionValidation
{
    [MenuItem("Tools/Validation/Quest Completion")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode 전용입니다.");
        int count = 0, saves = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); count++; };
        var bindings = new Dictionary<FieldInfo, object>();
        // 실제 씬의 저장·UI 구독자는 호출하지 않고 finally에서 원복한다.
        foreach (var type in new[] { typeof(GameEvent), typeof(PlayerEvent), typeof(CurrencyEvent), typeof(RewardEvent) })
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                if (typeof(Delegate).IsAssignableFrom(field.FieldType)) { bindings[field] = field.GetValue(null); field.SetValue(null, null); }
        var root = new GameObject("QuestCompletionFixture") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        var node = ScriptableObject.CreateInstance<EventNode>(); node.name = "QuestFixture";
        var definition = ScriptableObject.CreateInstance<EventDefinition>(); node.definition = definition;
        var exit = ScriptableObject.CreateInstance<StoryNode>(); definition.exitNode = exit;
        var potion = ScriptableObject.CreateInstance<PotionItem>(); potion.itemID = "QuestPotion"; potion.itemName = "검사 물약"; potion.quantity = 2;
        var rewardItem = ScriptableObject.CreateInstance<PotionItem>(); rewardItem.itemID = "QuestReward"; rewardItem.itemName = "보상 물약";
        try
        {
            var game = Bind<GameManager>(root, bindings); game.eventProgress = new EventProgress();
            var manager = Bind<EventManager>(root, bindings);
            var nodes = Bind<NodeManager>(root, bindings);
            var runner = new NodeRunner(); runner.TryEnter(node); Set(nodes, "nodeRunner", runner);
            var characters = Bind<CharacterManager>(root, bindings);
            var player = root.AddComponent<Player>(); player.InitializeFromData(new PlayerData { baseStats = new BaseStats(1, 1, 1) });
            typeof(CharacterManager).GetProperty("currentPlayer").SetValue(characters, player);
            var inventory = Bind<InventoryManager>(root, bindings);
            var currency = Bind<CurrencyManager>(root, bindings); currency.currencyList.Add(new CurrencyData("Gold", 50));
            var accept = new EventOption { id = "accept", text = "수락", action = EventActionKind.AcceptQuest, questId = "Q" };
            var complete = new EventOption { id = "complete", text = "완료", action = EventActionKind.CompleteQuest, questId = "Q",
                requiredQuest = "Q", requiredQuestState = QuestRequirementState.Active, requiredItem = potion, requiredQuantity = 2,
                requiredGold = 50, requiredStats = new BaseStats(1,1,1), tendencyCondition = TendencyRequirement.AtLeast, tendencyMin = 1,
                reward = new Reward { dropGold = 200 }, resultText = "준비를 마쳐 퀘스트를 완료하였다.", nextNode = exit };
            definition.options.Add(accept); definition.options.Add(complete);
            GameEvent.OnSaveGame += () => saves++;
            var result = manager.Execute(node, accept);
            check(result.success && result.completedQuestOption == null && game.eventProgress.acceptedQuests.Contains("Q"), "미충족이면 수락만 처리");
            check(!game.eventProgress.completedQuests.Contains("Q") && currency.GetAmount("Gold") == 50, "미충족 보상 미지급");
            check(!manager.Execute(node, complete).success, "완료 직접 실행도 조건 재검사");
            inventory.inventoryItems.Add(potion); player.ChangeTendency(1);
            check(manager.GetUnavailableReason(node, complete) == null, "수락 이후 조건 달성 시 완료 선택지 해금");

            Action reset = () => { game.eventProgress = new EventProgress(); currency.currencyList[0].SetAmount(50); saves = 0; };
            reset();
            result = manager.Execute(node, accept);
            check(result.success && game.eventProgress.completedQuests.Contains("Q"), "수락 즉시 자동 완료");
            check(currency.GetAmount("Gold") == 250 && potion.quantity == 2, "보상 한 번 지급·보유 조건 아이템 무소비");
            check(result.completedQuestOption == complete && result.completedQuestOption.nextNode == exit, "완료 문구·이동 선택지 반환");
            check(result.message.Contains("퀘스트 완료: Q") && saves == 1, "완료 로그 및 단일 저장");
            check(game.eventProgress.claimed.Contains("QuestFixture/accept") && game.eventProgress.claimed.Contains("QuestFixture/complete"), "수락·완료 수령 기록");
            check(!manager.Execute(node, accept).success && !manager.Execute(node, complete).success && currency.GetAmount("Gold") == 250, "중복 수락·완료 보상 방지");

            reset(); potion.quantity = 1;
            check(manager.Execute(node, accept).completedQuestOption == null, "아이템 수량 미달 자동 완료 차단"); potion.quantity = 2;
            reset(); currency.currencyList[0].SetAmount(49);
            check(manager.Execute(node, accept).completedQuestOption == null, "골드 미달 자동 완료 차단");
            reset(); player.ChangeTendency(-1);
            check(manager.Execute(node, accept).completedQuestOption == null, "성향 미달 자동 완료 차단"); player.ChangeTendency(1);
            reset(); complete.requiredStats = new BaseStats(2,1,1);
            check(manager.Execute(node, accept).completedQuestOption == null, "STR 미달 자동 완료 차단");
            reset(); complete.requiredStats = new BaseStats(1,2,1);
            check(manager.Execute(node, accept).completedQuestOption == null, "DEX 미달 자동 완료 차단");
            reset(); complete.requiredStats = new BaseStats(1,1,2);
            check(manager.Execute(node, accept).completedQuestOption == null, "CON 미달 자동 완료 차단"); complete.requiredStats = new BaseStats(1,1,1);
            reset(); complete.questId = "Other";
            check(manager.Execute(node, accept).completedQuestOption == null, "다른 퀘스트 완료 선택지 무시"); complete.questId = "Q";
            reset();
            while (inventory.inventoryItems.Count < inventory.Capacity) inventory.inventoryItems.Add(potion);
            complete.reward = new Reward { dropGold = 200, items = new List<ItemReward> { new ItemReward { item = rewardItem, quantity = 1, probability = 1 } } };
            result = manager.Execute(node, accept);
            check(result.completedQuestOption == null && !game.eventProgress.completedQuests.Contains("Q") && currency.GetAmount("Gold") == 50, "가방 부족이면 수락 유지·완료 보상 미지급");
            inventory.inventoryItems.Clear(); inventory.inventoryItems.Add(potion);
            check(manager.Execute(node, complete).success && inventory.inventoryItems.Count == 2, "가방 확보 후 완료 보상 재시도");
            reset(); complete.reward = new Reward { dropGold = int.MaxValue };
            result = manager.Execute(node, accept);
            check(result.completedQuestOption == null && !game.eventProgress.completedQuests.Contains("Q") && currency.GetAmount("Gold") == 50, "지급 오류이면 완료·보상 기록 없음");
            complete.reward = new Reward { dropGold = 200 };
            check(manager.Execute(node, complete).success && currency.GetAmount("Gold") == 250, "지급 오류 해소 후 완료 재시도");
            var saved = new SaveGameData { player = PlayerSaveData.FromPlayerData(player.GetCurrentData()), eventProgress = game.eventProgress };
            check(SaveGameData.TryDeserialize(JsonUtility.ToJson(saved), out var restored, out _) && restored.eventProgress.completedQuests.Contains("Q"), "완료 상태 저장 왕복");
            return "Quest completion: " + count + " passed (실제 세이브 변경 없음)";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(node); UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(exit); UnityEngine.Object.DestroyImmediate(potion);
            UnityEngine.Object.DestroyImmediate(rewardItem);
            foreach (var binding in bindings) binding.Key.SetValue(null, binding.Value);
        }
    }

    private static T Bind<T>(GameObject root, Dictionary<FieldInfo, object> bindings) where T : MonoBehaviour
    {
        var field = typeof(SingleTon<T>).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        bindings[field] = field.GetValue(null);
        var component = root.AddComponent<T>(); field.SetValue(null, component); return component;
    }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
}
