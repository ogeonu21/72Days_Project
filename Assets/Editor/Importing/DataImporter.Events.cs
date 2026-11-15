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
        foreach (var row in SheetJson.Read<EventDataRaw>(events, true))
            CheckType("EventDefinitions", row.EventID, typeof(EventDefinition));
        return RuntimeEventBuilder.BuildEventDefinitions(events, choices, rewards, findItem, findNode);
    }

    private static float ParseEventProbability(string value, string id)
    {
        if (string.IsNullOrWhiteSpace(value)) return 1f;
        if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float probability))
            throw new InvalidOperationException(id + ": SuccessProbability는 0~1 숫자여야 합니다.");
        CheckRate(probability, id + " SuccessProbability");
        return probability;
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
