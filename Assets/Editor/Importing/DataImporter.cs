using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public partial class DataImporter : EditorWindow
{
    private const string baseURL = "https://script.google.com/macros/s/AKfycbzd47uZXwCe472lYE247EzI4jabj9fhZOIRgXUVL6f74683LiMpo7T4NnMFDc2G76VG-Q/exec";    
    private static Dictionary<string, Node> nodeMap = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);

    [MenuItem("Tools/Update All Game Data")]
    public static async void UpdateAllGameData()
    {
        if (importing || EditorApplication.isPlayingOrWillChangePlaymode) return;
        importing = true;
        try
        {
            string items = await Fetch("ItemData");
            string stats = await Fetch("StatusData");
            string nodes = await Fetch("NodeData");
            string events = await Fetch("EventData");
            string choices = await Fetch("EventChoiceData");
            string rewards = await Fetch("RewardData");
            ValidateSnapshot(items, stats, nodes, events, choices, rewards);
            if (!EditorUtility.DisplayDialog("데이터 가져오기 미리보기",
                $"아이템 {ParseRows<ItemDataRaw>(items).Length}개, 캐릭터 {ParseRows<StatusDataRaw>(stats).Length}개, 노드 {ParseRows<NodeDataRaw>(nodes).Length}개, 이벤트 {SheetJson.Read<EventDataRaw>(events).Length}개 갱신\n시트에 없는 에셋은 보존. 기존 GUID 유지. 적용할까요?", "적용", "취소")) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ApplySnapshot(items, stats, nodes, events, choices, rewards);
            Debug.Log("[DataImporter] 데이터 적용 완료.");
        }
        catch (Exception ex) { Debug.LogError("[DataImporter] 가져오기 실패: " + ex.Message); }
        finally { importing = false; }
    }
    private static bool importing;
    public static string LastValidationReport { get; private set; } = "검사 전";

    [MenuItem("Tools/Validate Remote Game Data (No Changes)")]
    public static async void ValidateRemoteGameData()
    {
        if (importing || EditorApplication.isPlayingOrWillChangePlaymode) return;
        importing = true;
        LastValidationReport = "원격 시트 검사 중";
        try
        {
            var data = await Task.WhenAll(Fetch("ItemData"), Fetch("StatusData"), Fetch("NodeData"),
                Fetch("EventData"), Fetch("EventChoiceData"), Fetch("RewardData"));
            ValidateSnapshot(data[0], data[1], data[2], data[3], data[4], data[5]);
            LastValidationReport = $"원격 6개 시트 검증 통과: 아이템 {ParseRows<ItemDataRaw>(data[0]).Length}, 적 {ParseRows<StatusDataRaw>(data[1]).Length}, 노드 {ParseRows<NodeDataRaw>(data[2]).Length}, 이벤트 {SheetJson.Read<EventDataRaw>(data[3]).Length}, 선택지 {SheetJson.Read<EventChoiceDataRaw>(data[4]).Length}, 보상 행 {SheetJson.Read<RewardDataRaw>(data[5]).Length}. 에셋 변경 없음.";
            Debug.Log("[DataImporter] " + LastValidationReport);
        }
        catch (Exception ex) { LastValidationReport = "검증 실패: " + ex.Message; Debug.LogError("[DataImporter] " + LastValidationReport); }
        finally { importing = false; }
    }

    public static void ApplySnapshot(string items, string stats, string nodes, string events, string choices, string rewards)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode에서만 가져올 수 있습니다.");
        ValidateSnapshot(items, stats, nodes, events, choices, rewards);
        var newPaths = new List<string>();
        foreach (var row in ParseRows<ItemDataRaw>(items)) TrackNewAsset(newPaths, "Items", row.ItemID);
        foreach (var row in ParseRows<StatusDataRaw>(stats)) TrackNewAsset(newPaths, "Characters", row.ID);
        foreach (var row in ParseRows<NodeDataRaw>(nodes)) TrackNewAsset(newPaths, "Nodes", row.NodeID);
        foreach (var row in SheetJson.Read<EventDataRaw>(events)) TrackNewAsset(newPaths, "EventDefinitions", row.EventID);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("게임 데이터 가져오기");
        try
        {
            ImportItems(items);
            ImportStats(stats);
            CreateOrReconstructNodeAssets(nodes);
            RebuildNodeMap();
            ImportEventDefinitions(events, choices, rewards);
            LinkAllReferences(nodes);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            foreach (var path in newPaths) if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            throw;
        }
    }

    private static void TrackNewAsset(List<string> paths, string folder, string id)
    {
        string path = $"Assets/Resources/{folder}/{id}.asset";
        if (!File.Exists(path)) paths.Add(path);
    }

    // Validate the entire snapshot before touching any asset.
    public static void ValidateSnapshot(string itemJson, string statusJson, string nodeJson,
        string eventJson = null, string choiceJson = null, string rewardJson = null)
    {
        var items = ParseRows<ItemDataRaw>(itemJson);
        var stats = ParseRows<StatusDataRaw>(statusJson);
        var nodes = ParseRows<NodeDataRaw>(nodeJson);
        var itemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var enemyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> sheetEventIds = ValidateEventSheets(items, nodes, eventJson, choiceJson, rewardJson);
        foreach (var row in items)
        {
            CheckId(row?.ItemID, itemIds, "Items");
            var sample = CreateItemInstance(row.ItemCategory);
            if (sample == null) throw new InvalidOperationException(row.ItemID + ": 잘못된 아이템 종류");
            try { CheckType("Items", row.ItemID, sample.GetType()); }
            finally { DestroyImmediate(sample); }
            ValidateItemMapping(row);
            if (row.ItemCategory == "Armor" && (!Enum.TryParse(row.ArmorType, out ArmorType armor) || !Enum.IsDefined(typeof(ArmorType), armor)))
                throw new InvalidOperationException(row.ItemID + ": 잘못된 방어구 종류");
        }
        foreach (var row in stats)
        {
            CheckId(row?.ID, enemyIds, "Characters");
            CheckType("Characters", row.ID, typeof(EnemyData));
            ValidateStatusMapping(row, items);
            if (float.IsNaN(row.ItemDropRate) || float.IsInfinity(row.ItemDropRate) || row.ItemDropRate < 0 || row.ItemDropRate > 1 || (row.ItemDropRate > 0 && string.IsNullOrWhiteSpace(row.DropItemID)))
                throw new InvalidOperationException(row.ID + ": 드롭 확률 또는 아이템 설정 오류");
            if (!string.IsNullOrEmpty(row.DropItemID) && !itemIds.Contains(row.DropItemID) && Resources.Load<BaseItem>("Items/" + row.DropItemID) == null)
                throw new InvalidOperationException(row.ID + ": 드롭 아이템 누락 " + row.DropItemID);
        }
        foreach (var row in nodes)
        {
            CheckId(row?.NodeID, nodeIds, "Nodes");
            if (!Enum.TryParse(row.NodeType, out NodeType type) || !Enum.IsDefined(typeof(NodeType), type) || type.ToString() != row.NodeType)
                throw new InvalidOperationException(row.NodeID + ": 잘못된 노드 종류");
            var sample = CreateNodeInstance(type);
            try { CheckType("Nodes", row.NodeID, sample.GetType()); }
            finally { DestroyImmediate(sample); }
            if (!Enum.TryParse(row.WorldLocation, out WorldLocation location) || !Enum.IsDefined(typeof(WorldLocation), location))
                throw new InvalidOperationException(row.NodeID + ": 잘못된 지역");
            if (row.SurviveDate < 0) throw new InvalidOperationException(row.NodeID + ": 날짜는 음수일 수 없습니다.");
        }
        foreach (var row in nodes)
        {
            if (row.NodeType == nameof(NodeType.MainStoryNode)) CheckLink(row.NodeID, row.NextNode, nodeIds);
            if (row.NodeType == nameof(NodeType.CombatNode))
            {
                CheckLink(row.NodeID, row.SuccessNode, nodeIds);
                CheckLink(row.NodeID, row.FailureNode, nodeIds);
                if (string.IsNullOrWhiteSpace(row.CombatEnemyID) || (!enemyIds.Contains(row.CombatEnemyID) && Resources.Load<EnemyData>("Characters/" + row.CombatEnemyID) == null))
                    throw new InvalidOperationException(row.NodeID + ": 전투 적 누락");
            }
            if (row.NodeType != nameof(NodeType.StoryNode) && row.NodeType != nameof(NodeType.EventNode)) continue;
            if (row.NodeType == nameof(NodeType.EventNode))
            {
                if (string.IsNullOrWhiteSpace(row.EventDefinitionID)) throw new InvalidOperationException(row.NodeID + ": EventDefinitionID 필수. 구형 이벤트는 지원하지 않습니다.");
                if (!string.IsNullOrWhiteSpace(row.Choice1_Text) || !string.IsNullOrWhiteSpace(row.Choice2_Text) || !string.IsNullOrWhiteSpace(row.Choice3_Text))
                    throw new InvalidOperationException(row.NodeID + ": EventNode 선택지는 EventChoiceData에서만 작성하세요.");
                if (sheetEventIds.Contains(row.EventDefinitionID)) continue;
                var definition = Resources.Load<EventDefinition>("EventDefinitions/" + row.EventDefinitionID.Trim());
                if (definition == null) throw new InvalidOperationException(row.NodeID + ": EventDefinition 누락 " + row.EventDefinitionID);
                string definitionError = EventDefinitionValidator.Validate(definition);
                if (definitionError != null) throw new InvalidOperationException(row.NodeID + ": " + definitionError);
                continue;
            }
            string[] labels = { row.Choice1_Text, row.Choice2_Text, row.Choice3_Text };
            string[] links = { row.Choice1_NextNode, row.Choice2_NextNode, row.Choice3_NextNode };
            int count = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(labels[i]))
                {
                    if (!string.IsNullOrEmpty(links[i])) throw new InvalidOperationException(row.NodeID + ": 선택지 문구 누락");
                    continue;
                }
                count++;
                CheckLink(row.NodeID, links[i], nodeIds);
            }
            if (count == 0) throw new InvalidOperationException(row.NodeID + ": 선택지 없음");
        }
    }

    private static T[] ParseRows<T>(string json)
    {
        var rows = SheetJson.Read<T>(json, true);
        if (rows == null || rows.Length == 0) throw new InvalidOperationException("빈 시트는 적용하지 않습니다.");
        return rows;
    }

    private static void CheckId(string id, HashSet<string> ids, string folder)
    {
        if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || id == "." || id == ".." || !ids.Add(id))
            throw new InvalidOperationException(folder + ": 잘못되었거나 중복된 ID " + id);
    }

    private static void CheckType(string folder, string id, Type type)
    {
        string path = $"Assets/Resources/{folder}/{id}.asset";
        var existing = AssetDatabase.LoadMainAssetAtPath(path);
        if (existing != null && existing.GetType() != type) throw new InvalidOperationException(path + ": 타입 변경 불가 (GUID 보호)");
    }

    private static void CheckLink(string source, string target, HashSet<string> ids)
    {
        if (string.IsNullOrWhiteSpace(target) || (!ids.Contains(target) && Resources.Load<Node>("Nodes/" + target) == null))
            throw new InvalidOperationException("Assets/Resources/Nodes/" + source + ".asset: 연결 누락 " + target);
    }
    #region [Helpers]
    private static async Task<string> Fetch(string sheetName)
    {
        string url = $"{baseURL}?sheetName={sheetName}";
        using UnityWebRequest www = UnityWebRequest.Get(url);
        www.timeout = 30;
        var op = www.SendWebRequest();
        while (!op.isDone) await Task.Delay(50);
        if (www.result != UnityWebRequest.Result.Success)
            throw new InvalidOperationException(sheetName + ": " + www.error);
        return www.downloadHandler.text;
    }
    #endregion
    
    #region [Import Node]

    private static void RunImportSequence(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        CreateOrReconstructNodeAssets(json);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // [단계 2] 맵 재구축 (참조 연결을 위해 메모리에 로드)
        RebuildNodeMap();

        // [단계 3] 클래스별 전용 필드 및 노드 간 참조 연결
        LinkAllReferences(json);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateOrReconstructNodeAssets(string json)
    {
        var nodeDataList = ParseRows<NodeDataRaw>(json);
        string folderPath = "Assets/Resources/Nodes";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        foreach (var data in nodeDataList)
        {
            if (string.IsNullOrEmpty(data.NodeID)) continue;

            string path = $"{folderPath}/{data.NodeID}.asset";
            if (!System.Enum.TryParse(data.NodeType, out NodeType targetType)) continue;

            Node existingNode = AssetDatabase.LoadAssetAtPath<Node>(path);

            if (existingNode != null && existingNode.GetType().Name != targetType.ToString())
            {
                Debug.Log($"<color=yellow>[Replace]</color> {data.NodeID}: {existingNode.GetType().Name} -> {targetType}");
                throw new InvalidOperationException(path + ": 노드 타입 변경은 별도 마이그레이션이 필요합니다.");
            }

            if (existingNode == null)
            {
                existingNode = CreateNodeInstance(targetType);
                AssetDatabase.CreateAsset(existingNode, path);
                Undo.RegisterCreatedObjectUndo(existingNode, "노드 생성");
            }

            Undo.RecordObject(existingNode, "노드 갱신");
            existingNode.nodeType = targetType; // 인스펙터 변수 할당
            existingNode.nodeName = data.NodeID;
            existingNode.nodeMessage = SheetJson.Multiline(data.NodeMessage);
            existingNode.surviveDate = data.SurviveDate;

            if (System.Enum.TryParse(data.WorldLocation, out WorldLocation loc))
                existingNode.worldLocation = loc;

            if (existingNode is CombatNode cn) cn.combatEnemyID = data.CombatEnemyID;
            else if (existingNode is EndingNode edn) edn.endingName = data.EndingName;

            EditorUtility.SetDirty(existingNode);
        }
    }

    private static void LinkAllReferences(string json)
    {
        var nodeDataList = ParseRows<NodeDataRaw>(json);
        foreach (var data in nodeDataList)
        {
            if (!nodeMap.TryGetValue(data.NodeID, out Node node)) continue;
            Undo.RecordObject(node, "노드 연결");

            // 클래스 타입에 맞춰 정확한 참조 필드 연결
            switch (node)
            {
                case MainStoryNode mn:
                    mn.nextNode = FindNode(data.NextNode);
                    break;
                case CombatNode cn:
                    cn.enemyData = Resources.Load<EnemyData>($"Characters/{data.CombatEnemyID}");
                    cn.successNode = FindNode(data.SuccessNode);
                    cn.failureNode = FindNode(data.FailureNode);
                    break;
                case StoryNode sn:
                    sn.choices = CreateChoiceList(data);
                    break;
                case EventNode en:
                    en.definition = string.IsNullOrWhiteSpace(data.EventDefinitionID) ? null : Resources.Load<EventDefinition>("EventDefinitions/" + data.EventDefinitionID.Trim());
                    break;
            }
            EditorUtility.SetDirty(node);
        }
    }

    private static void RebuildNodeMap()
    {
        nodeMap.Clear();
        string path = "Assets/Resources/Nodes";
        if (!Directory.Exists(path)) return;

        string[] files = Directory.GetFiles(path, "*.asset");
        foreach (string file in files)
        {
            Node n = AssetDatabase.LoadAssetAtPath<Node>(file);
            if (n != null) nodeMap[n.name] = n;
        }
    }

    

    #region [Node Helpers]
    private static List<Choice> CreateChoiceList(NodeDataRaw data)
    {
        List<Choice> list = new List<Choice>();
        AddChoice(list, data.Choice1_Text, data.Choice1_NextNode);
        AddChoice(list, data.Choice2_Text, data.Choice2_NextNode);
        AddChoice(list, data.Choice3_Text, data.Choice3_NextNode);
        return list;
    }

    private static void AddChoice(List<Choice> list, string txt, string nxtID)
    {
        if (string.IsNullOrEmpty(txt)) return;
        Choice c = new Choice 
        { 
            choiceText = SheetJson.Multiline(txt),
            nextNode = FindNode(nxtID)
        };
        list.Add(c);
    }

    private static Node FindNode(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return nodeMap.TryGetValue(id.Trim(), out Node result) ? result : Resources.Load<Node>("Nodes/" + id.Trim());
    }

    
    private static Node CreateNodeInstance(NodeType type)
    {
        return type switch
        {
            NodeType.MainStoryNode => CreateInstance<MainStoryNode>(),
            NodeType.StoryNode => CreateInstance<StoryNode>(),
            NodeType.CombatNode => CreateInstance<CombatNode>(),
            NodeType.EventNode => CreateInstance<EventNode>(),
            NodeType.EndingNode => CreateInstance<EndingNode>(),
            _ => CreateInstance<MainStoryNode>()
        };
    }
    #endregion
    #endregion

    #region [Import Stats]
    private static void ImportStats(string json)
{
    var stats = ParseRows<StatusDataRaw>(json);
    EnsureResourceFolder("Characters");
    foreach (var data in stats)
    {
        if (string.IsNullOrEmpty(data.ID)) continue;
        
        string path = $"Assets/Resources/Characters/{data.ID}.asset";
        EnemyData def = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(def, path);
            Undo.RegisterCreatedObjectUndo(def, "캐릭터 생성");
        }
        Undo.RecordObject(def, "캐릭터 갱신");

        // 기본 정보 갱신
        def.type = data.Type; 
        def.id = data.ID; 
        def.displayName = data.Name;
        def.baseStats = new BaseStats(data.STR, data.DEX, data.CON);
        def.tuningStats = new TuningStats(data.AttackBonus, data.HpBonus, data.DodgeBonus, data.RangeBonus);

        // --- [드랍 아이템 연결 로직: 연결만 수행] ---
        if (!string.IsNullOrEmpty(data.DropItemID))
        {
            string itemPath = $"Assets/Resources/Items/{data.DropItemID}.asset";
            BaseItem itemAsset = AssetDatabase.LoadAssetAtPath<BaseItem>(itemPath);

            if (itemAsset != null)
            {
                def.dropItem = itemAsset;
            }
            else
            {
                // 아이템 에셋 자체가 없는 경우
                Debug.LogWarning($"<color=orange>[Missing Item]</color> {data.ID}의 드랍 아이템 {data.DropItemID} 에셋을 찾을 수 없습니다. ItemData를 먼저 임포트했는지 확인하세요.");
                def.dropItem = null;
            }
            
            def.itemDropRate = data.ItemDropRate;
        }
        else
        {
            def.dropItem = null; // 아이템 ID가 비어있으면 참조 제거
        }

        def.itemDropRate = data.ItemDropRate;
        def.dropGold = data.DropGold;
        EditorUtility.SetDirty(def);
    }
}

    
    #endregion
    #region [Import Items]
    private static void ImportItems(string json)
    {
        var items = ParseRows<ItemDataRaw>(json);
        EnsureResourceFolder("Items");
        foreach(var data in items)
        {
            if (string.IsNullOrEmpty(data.ItemID)) continue;
            string path = $"Assets/Resources/Items/{data.ItemID}.asset";
            BaseItem item = AssetDatabase.LoadAssetAtPath<BaseItem>(path);
            if (item != null && item.GetType().Name != data.ItemCategory + "Item")
            {
                throw new InvalidOperationException(path + ": 아이템 타입 변경은 별도 마이그레이션이 필요합니다.");
            }
            if (item == null)
            {
                item = CreateItemInstance(data.ItemCategory);
                AssetDatabase.CreateAsset(item, path);
                Undo.RegisterCreatedObjectUndo(item, "아이템 생성");
            }
            Undo.RecordObject(item, "아이템 갱신");
            item.itemID = data.ItemID;
            item.itemName = data.ItemName;
            item.itemDescription = SheetJson.Multiline(data.ItemDesc);
            item.itemCategory = System.Enum.TryParse(data.ItemCategory, out ItemCategory cat) ? cat : ItemCategory.Weapon;
            // 빈 시트 값으로 에디터에서 지정한 주소를 지우지 않는다.
            if (!string.IsNullOrWhiteSpace(data.ItemIcon)) item.itemIcon = data.ItemIcon.Trim();
            item.isConsumable = data.Consumable;
            item.itemValue = data.ItemValue;
            
            if (item is WeaponItem w)
            {
                w.durability = data.Durability;
                w.attackBonus = data.AttackBonus;
                w.range = data.Range;
            }
            else if (item is ArmorItem a)
            {
                a.durability = data.Durability;
                a.armorType = System.Enum.TryParse(data.ArmorType, out ArmorType at) ? at : ArmorType.Helmet;
                a.hpBonus = data.HpBonus;
                a.dodgeBonus = data.DodgeBonus;
            }
            else if (item is AccessoryItem ac)
            {
                ac.durability = data.Durability;
                ac.dodgeBonus = data.DodgeBonus;
                ac.questID = data.QuestID;
            }
            else if (item is PotionItem p)
            {
                p.health = data.Health;
            }

            EditorUtility.SetDirty(item);
        }
    }

    private static BaseItem CreateItemInstance(string categoryStr)
    {
        if (!System.Enum.TryParse(categoryStr, out ItemCategory category) || category.ToString() != categoryStr) return null;

        BaseItem item = category switch
        {
            ItemCategory.Weapon => CreateInstance<WeaponItem>(),
            ItemCategory.Armor => CreateInstance<ArmorItem>(),
            ItemCategory.Accessory => CreateInstance<AccessoryItem>(),
            ItemCategory.Potion => CreateInstance<PotionItem>(),
            _ => null
        };
        return item;
    }
    #endregion
}
