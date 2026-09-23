using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class DataImporter
{
    private static void EnsureResourceFolder(string name)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/" + name)) AssetDatabase.CreateFolder("Assets/Resources", name);
    }

    private static void ValidateItemMapping(ItemDataRaw row)
    {
        if (row.Consumable != (row.ItemCategory == "Potion")) throw new InvalidOperationException(row.ItemID + ": Consumable은 Potion만 TRUE입니다.");
        if (row.ItemValue < 0 || row.Durability < 0 || row.AttackBonus < 0 || row.Range < 0 || row.HpBonus < 0 || row.Health < 0)
            throw new InvalidOperationException(row.ItemID + ": 아이템 수치는 음수일 수 없습니다.");
        CheckRate(row.DodgeBonus, row.ItemID + " DodgeBonus");
    }

    private static void ValidateStatusMapping(StatusDataRaw row, ItemDataRaw[] items)
    {
        if (row.STR < 0 || row.DEX < 0 || row.CON < 0 || row.DropGold < 0) throw new InvalidOperationException(row.ID + ": 능력치/골드는 음수일 수 없습니다.");
        CheckRate(row.DodgeBonus, row.ID + " DodgeBonus");
        if (!string.IsNullOrWhiteSpace(row.DropItemCategory))
        {
            var source = items.FirstOrDefault(i => i.ItemID == row.DropItemID);
            string category = source?.ItemCategory ?? Resources.Load<BaseItem>("Items/" + row.DropItemID)?.itemCategory.ToString();
            if (category != row.DropItemCategory) throw new InvalidOperationException(row.ID + ": DropItemCategory와 DropItemID 종류가 다릅니다.");
        }
    }

    private static void CheckRate(float rate, string label)
    {
        if (float.IsNaN(rate) || float.IsInfinity(rate) || rate < 0 || rate > 1) throw new InvalidOperationException(label + ": 0~1 범위가 필요합니다.");
    }

    private static T NamedEnum<T>(string value, string context) where T : struct
    {
        if (!Enum.TryParse(value, out T parsed) || !Enum.IsDefined(typeof(T), parsed) || parsed.ToString() != value)
            throw new InvalidOperationException(context + ": 잘못된 유형 " + value);
        return parsed;
    }

    private static HashSet<string> ValidateEventSheets(ItemDataRaw[] items, NodeDataRaw[] nodes, string events, string choices, string rewards)
    {
        if (events == null && choices == null && rewards == null) return new HashSet<string>();
        // 아직 생성되지 않은 노드/아이템도 동일 스냅샷으로 검증한다. 실제 에셋에는 쓰지 않는다.
        var temporary = new List<UnityEngine.Object>();
        var itemLookup = new Dictionary<string, BaseItem>(StringComparer.Ordinal);
        var nodeLookup = new Dictionary<string, Node>(StringComparer.Ordinal);
        try
        {
            foreach (var row in items)
            {
                var item = CreateItemInstance(row.ItemCategory);
                if (item == null) throw new InvalidOperationException(row.ItemID + ": 아이템 유형 오류");
                temporary.Add(item); item.itemID = row.ItemID;
                itemLookup.Add(row.ItemID, item);
            }
            foreach (var row in nodes)
            {
                var node = CreateNodeInstance(NamedEnum<NodeType>(row.NodeType, row.NodeID));
                temporary.Add(node); nodeLookup.Add(row.NodeID, node);
            }
            var built = BuildEventDefinitions(events, choices, rewards,
                id => itemLookup.TryGetValue(id, out var item) ? item : Resources.Load<BaseItem>("Items/" + id),
                id => nodeLookup.TryGetValue(id, out var node) ? node : Resources.Load<Node>("Nodes/" + id));
            temporary.AddRange(built.Values);
            return new HashSet<string>(built.Keys, StringComparer.Ordinal);
        }
        finally { foreach (var obj in temporary) if (obj != null) DestroyImmediate(obj); }
    }

    public static Dictionary<string, EventDefinition> BuildEventDefinitions(string events, string choices, string rewards,
        Func<string, BaseItem> findItem, Func<string, Node> findNode)
    {
        var eventRows = SheetJson.Read<EventDataRaw>(events, true);
        var choiceRows = SheetJson.Read<EventChoiceDataRaw>(choices, true);
        var rewardRows = SheetJson.Read<RewardDataRaw>(rewards, true);
        var result = new Dictionary<string, EventDefinition>(StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var choiceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rewardMap = new Dictionary<string, Reward>(StringComparer.Ordinal);
        try
        {
            foreach (var row in rewardRows)
            {
                CheckId(row.RewardID, new HashSet<string>(), "RewardData RewardID");
                CheckId(row.EntryID, new HashSet<string>(), "RewardData EntryID");
                if (!entryIds.Add(row.RewardID + "/" + row.EntryID)) throw new InvalidOperationException(row.RewardID + ": 보상 EntryID 중복");
                if (row.Amount < 1) throw new InvalidOperationException(row.RewardID + ": Amount는 1 이상이어야 합니다.");
                CheckRate(row.Probability, row.RewardID + " Probability");
                if (!rewardMap.TryGetValue(row.RewardID, out var reward)) reward = new Reward { items = new List<ItemReward>() };
                if (row.Kind == "Item")
                {
                    BaseItem item = string.IsNullOrWhiteSpace(row.ItemID) ? null : findItem(row.ItemID);
                    if (item == null) throw new InvalidOperationException(row.RewardID + ": 아이템 누락 " + row.ItemID);
                    reward.items.Add(new ItemReward { item = item, quantity = row.Amount, probability = row.Probability });
                }
                else
                {
                    if (row.Probability != 1 || !string.IsNullOrWhiteSpace(row.ItemID)) throw new InvalidOperationException(row.RewardID + ": 비아이템 보상은 Probability=1, ItemID는 빈 값이어야 합니다.");
                    checked
                    {
                        switch (row.Kind)
                        {
                            case "Gold": reward.dropGold += row.Amount; break;
                            case "Heal": reward.hpHeal += row.Amount; break;
                            case "Exp": reward.exp += row.Amount; break;
                            case "STR": reward.statIncrease.str += row.Amount; break;
                            case "DEX": reward.statIncrease.dex += row.Amount; break;
                            case "CON": reward.statIncrease.con += row.Amount; break;
                            default: throw new InvalidOperationException(row.RewardID + ": 보상 종류 오류 " + row.Kind);
                        }
                    }
                }
                string error = RewardService.Validate(reward);
                if (error != null) throw new InvalidOperationException(row.RewardID + ": " + error);
                rewardMap[row.RewardID] = reward;
            }
            foreach (var row in eventRows)
            {
                CheckId(row.EventID, ids, "EventData");
                CheckType("EventDefinitions", row.EventID, typeof(EventDefinition));
                var definition = CreateInstance<EventDefinition>();
                result.Add(row.EventID, definition);
                definition.name = row.EventID;
                definition.kind = NamedEnum<EventKind>(row.Kind, row.EventID);
                if (string.IsNullOrWhiteSpace(row.Title)) throw new InvalidOperationException(row.EventID + ": Title 필수");
                definition.title = SheetJson.Multiline(row.Title);
                definition.exitNode = string.IsNullOrWhiteSpace(row.ExitNodeID) ? null : findNode(row.ExitNodeID);
            }
            foreach (var row in choiceRows.OrderBy(c => c.SortOrder))
            {
                if (!result.TryGetValue(row.EventID, out var definition)) throw new InvalidOperationException(row.EventID + ": 선택지의 이벤트 누락");
                CheckId(row.ChoiceID, new HashSet<string>(), "ChoiceID");
                if (!choiceIds.Add(row.EventID + "/" + row.ChoiceID)) throw new InvalidOperationException(row.EventID + ": ChoiceID 중복");
                if (row.SortOrder < 0) throw new InvalidOperationException(row.EventID + ": SortOrder는 0 이상");
                var option = new EventOption
                {
                    id = row.ChoiceID, text = SheetJson.Multiline(row.Text), goldCost = row.GoldCost, goldLoss = row.GoldLoss,
                    repeatable = row.Repeatable, requiredQuest = row.RequiredQuestID,
                    requiredQuantity = row.RequiredQuantity, action = NamedEnum<EventActionKind>(row.Action, row.ChoiceID), questId = row.QuestID
                };
                if (!string.IsNullOrWhiteSpace(row.RequiredItemID))
                {
                    option.requiredItem = findItem(row.RequiredItemID);
                    if (option.requiredItem == null) throw new InvalidOperationException(row.ChoiceID + ": 조건 아이템 누락");
                }
                else if (row.RequiredQuantity != 0) throw new InvalidOperationException(row.ChoiceID + ": RequiredItemID 없이 수량만 입력됨");
                if (!string.IsNullOrWhiteSpace(row.NextNodeID))
                {
                    option.nextNode = findNode(row.NextNodeID);
                    if (option.nextNode == null) throw new InvalidOperationException(row.ChoiceID + ": 다음 노드 누락");
                }
                if (!string.IsNullOrWhiteSpace(row.RewardID))
                {
                    if (!rewardMap.TryGetValue(row.RewardID, out option.reward)) throw new InvalidOperationException(row.ChoiceID + ": RewardID 누락");
                }
                definition.options.Add(option);
            }
            foreach (var pair in result)
            {
                string error = EventDefinitionValidator.Validate(pair.Value);
                if (error != null) throw new InvalidOperationException(pair.Key + ": " + error);
            }
            return result;
        }
        catch { foreach (var obj in result.Values) DestroyImmediate(obj); throw; }
    }

    private static void ImportEventDefinitions(string events, string choices, string rewards)
    {
        EnsureResourceFolder("EventDefinitions");
        var definitions = BuildEventDefinitions(events, choices, rewards,
            id => Resources.Load<BaseItem>("Items/" + id), FindNode);
        try
        {
            foreach (var pair in definitions)
            {
                string path = "Assets/Resources/EventDefinitions/" + pair.Key + ".asset";
                var target = AssetDatabase.LoadAssetAtPath<EventDefinition>(path);
                if (target == null)
                {
                    target = CreateInstance<EventDefinition>();
                    AssetDatabase.CreateAsset(target, path);
                    Undo.RegisterCreatedObjectUndo(target, "이벤트 정의 생성");
                }
                Undo.RecordObject(target, "이벤트 정의 갱신");
                EditorUtility.CopySerialized(pair.Value, target);
                EditorUtility.SetDirty(target);
            }
        }
        finally { foreach (var obj in definitions.Values) DestroyImmediate(obj); }
    }
}
