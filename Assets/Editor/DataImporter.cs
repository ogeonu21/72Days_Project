using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;
using System.Collections.Generic;
using Codice.Client.Common.GameUI;

public class DataImporter : EditorWindow
{
    private const string baseURL = "https://script.google.com/macros/s/AKfycbzd47uZXwCe472lYE247EzI4jabj9fhZOIRgXUVL6f74683LiMpo7T4NnMFDc2G76VG-Q/exec";    
    private static Dictionary<string, Node> nodeMap = new Dictionary<string, Node>();

    [MenuItem("Tools/Update All Game Data")]
    public static void UpdateAllGameData()
    {
        FetchAndImport("StatusData", ImportStats);
        FetchAndImport("NodeData", RunImportSequence);
        FetchAndImport("ItemData", ImportItems);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=cyan><b>[System]</b> 게임 데이터 동기화 완료.</color>");
    }
    #region [Helpers]
    private static void FetchAndImport(string sheetName, System.Action<string> importAction)
    {
        string url = $"{baseURL}?sheetName={sheetName}";
        using UnityWebRequest www = UnityWebRequest.Get(url);
        var op = www.SendWebRequest();
        while (!op.isDone) { }
        if (www.result == UnityWebRequest.Result.Success)
            importAction(www.downloadHandler.text);
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
        var nodeDataList = JsonHelper.FromJson<NodeDataRaw>(json);
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
                AssetDatabase.DeleteAsset(path);
                existingNode = null;
            }

            if (existingNode == null)
            {
                existingNode = CreateNodeInstance(targetType);
                AssetDatabase.CreateAsset(existingNode, path);
            }

            existingNode.nodeType = targetType; // 인스펙터 변수 할당
            existingNode.nodeName = data.NodeID;
            existingNode.nodeMessage = data.NodeMessage;
            existingNode.surviveDate = data.SurviveDate;

            if (System.Enum.TryParse(data.WorldLocation, out WorldLocation loc))
                existingNode.worldLocation = loc;

            if (existingNode is CombatNode cn) cn.combatEnemyID = data.CombatEnemyID;
            else if (existingNode is EventNode en) en.eventCategory = data.EventCategory;
            else if (existingNode is EndingNode edn) edn.endingName = data.EndingName;

            EditorUtility.SetDirty(existingNode);
        }
    }

    private static void LinkAllReferences(string json)
    {
        var nodeDataList = JsonHelper.FromJson<NodeDataRaw>(json);
        foreach (var data in nodeDataList)
        {
            if (!nodeMap.TryGetValue(data.NodeID, out Node node)) continue;

            // 클래스 타입에 맞춰 정확한 참조 필드 연결
            switch (node)
            {
                case MainStoryNode mn:
                    mn.nextNode = FindNode(data.NextNode);
                    break;
                case CombatNode cn:
                    cn.enemyData = Resources.Load<EnemyDefinition>($"Characters/{data.CombatEnemyID}");
                    cn.successNode = FindNode(data.SuccessNode);
                    cn.failureNode = FindNode(data.FailureNode);
                    break;
                case StoryNode sn:
                    sn.choices = CreateChoiceList(data);
                    break;
                case EventNode en:
                    en.choices = CreateChoiceList(data);
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
        AddChoice(list, data.Choice1_Text, data.Choice1_NextNode, data.Choice1_EventName, data.EventCategory);
        AddChoice(list, data.Choice2_Text, data.Choice2_NextNode, data.Choice2_EventName, data.EventCategory);
        AddChoice(list, data.Choice3_Text, data.Choice3_NextNode, data.Choice3_EventName, data.EventCategory);
        return list;
    }

    private static void AddChoice(List<Choice> list, string txt, string nxtID, string evt, string cat)
    {
        if (string.IsNullOrEmpty(txt)) return;
        Choice c = new Choice 
        { 
            choiceText = txt, 
            nextNode = FindNode(nxtID)
        };
        if (!string.IsNullOrEmpty(evt))
            c.baseEvent = Resources.Load<BaseEvent>($"Events/{cat}/{evt}");
        list.Add(c);
    }

    private static Node FindNode(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return nodeMap.TryGetValue(id.Trim(), out Node result) ? result : null;
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
        var stats = JsonHelper.FromJson<StatusDataRaw>(json);
        foreach (var data in stats)
        {
            if (string.IsNullOrEmpty(data.ID)) continue;
            string path = $"Assets/Resources/Characters/{data.ID}.asset";
            EnemyDefinition def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.type = data.Type; def.id = data.ID; def.displayName = data.Name;
            def.baseStats = new BaseStats(data.STR, data.DEX, data.CON);
            def.tuningStats = new TuningStats(data.AttackBonus, data.HpBonus, data.DodgeBonus, data.RangeBonus);
            def.dropItemID = data.DropItemID;
            def.itemDropRate = data.ItemDropRate;
            def.dropGold = data.DropGold;
            EditorUtility.SetDirty(def);
        }
    }
    #endregion
    #region [Import Items]
    private static void ImportItems(string json)
    {
        var items = JsonHelper.FromJson<ItemDataRaw>(json);
        foreach(var data in items)
        {
            if (string.IsNullOrEmpty(data.ItemID)) continue;
            string path = $"Assets/Resources/Items/{data.ItemID}.asset";
            BaseItem item = AssetDatabase.LoadAssetAtPath<BaseItem>(path);
            if (item != null && item.GetType().Name != data.ItemCategory.ToString())
            {
                AssetDatabase.DeleteAsset(path);
                item = null;
            }
            if (item == null)
            {
                item = CreateItemInstance(data.ItemCategory);
                AssetDatabase.CreateAsset(item, path);
            }
            item.itemName = data.ItemName;
            item.itemDescription = data.ItemDesc;
            item.itemIcon = data.ItemIcon;
            item.isConsumable = data.Consumable;
            item.itemValue = data.ItemValue;
            
            if (item is WeaponItem w)
            {
                w.attackBonus = data.AttackBonus;
                w.range = data.Range;
            }
            else if (item is ArmorItem a)
            {
                a.hpBonus = data.HpBonus;
                a.dodgeBonus = data.DodgeBonus;
            }
            else if (item is AccessoryItem ac)
            {
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

        private static BaseItem CreateItemInstance(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Weapon => CreateInstance<WeaponItem>(),
                ItemCategory.Armor => CreateInstance<ArmorItem>(),
                ItemCategory.Accessory => CreateInstance<AccessoryItem>(),
                ItemCategory.Potion => CreateInstance<PotionItem>(),
                _ => CreateInstance<BaseItem>()
            };
        }
    #endregion
}