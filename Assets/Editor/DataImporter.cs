using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;
using System.Collections.Generic;

public class DataImporter : EditorWindow
{
    private const string baseURL = "https://script.google.com/macros/s/AKfycbzd47uZXwCe472lYE247EzI4jabj9fhZOIRgXUVL6f74683LiMpo7T4NnMFDc2G76VG-Q/exec";    
    private static Dictionary<string, Node> nodeMap = new Dictionary<string, Node>();

    [MenuItem("Tools/Update All Game Data")]
    public static void UpdateAllGameData()
    {
        // 1. 캐릭터 스탯 데이터 임포트
        FetchAndImport("StatusData", ImportStats);

        // 2. 노드 데이터 임포트 시퀀스 실행
        FetchAndImport("NodeData", RunImportSequence);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=cyan><b>[System]</b> 모든 데이터 동기화 및 NodeType 갱신 완료.</color>");
    }

    private static void RunImportSequence(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        // [단계 1] NodeType 일치 여부 확인 및 클래스 재생성
        // 여기서 인스펙터 창의 구성(StoryNode, CombatNode 등)이 결정됩니다.
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

            // 기존 에셋 로드 시도
            Node existingNode = AssetDatabase.LoadAssetAtPath<Node>(path);

            // [핵심] 클래스 타입이 시트의 NodeType과 다르면 삭제 후 재생성
            if (existingNode != null && existingNode.GetType().Name != targetType.ToString())
            {
                Debug.Log($"<color=yellow>[Replace]</color> {data.NodeID}: {existingNode.GetType().Name} -> {targetType}");
                AssetDatabase.DeleteAsset(path);
                existingNode = null;
            }

            // 에셋이 없으면 해당 클래스로 생성
            if (existingNode == null)
            {
                existingNode = CreateNodeInstance(targetType);
                AssetDatabase.CreateAsset(existingNode, path);
            }

            // [데이터 복구 및 주입]
            existingNode.nodeType = targetType; // 인스펙터 변수 할당
            existingNode.nodeName = data.NodeID;
            existingNode.nodeMessage = data.NodeMessage;
            existingNode.surviveDate = data.SurviveDate;

            if (System.Enum.TryParse(data.WorldLocation, out WorldLocation loc))
                existingNode.worldLocation = loc;

            // 자식 클래스 전용 단순 데이터 주입 (참조는 Link 단계에서)
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
            EditorUtility.SetDirty(def);
        }
    }
    #endregion

    #region [Helpers]
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
            nextNode = FindNode(nxtID), 
            triggersEvent = !string.IsNullOrEmpty(evt) 
        };
        if (c.triggersEvent)
            c.baseEvent = Resources.Load<BaseEvent>($"Events/{cat}/{evt}");
        list.Add(c);
    }

    private static Node FindNode(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return nodeMap.TryGetValue(id.Trim(), out Node result) ? result : null;
    }

    private static void FetchAndImport(string sheetName, System.Action<string> importAction)
    {
        string url = $"{baseURL}?sheetName={sheetName}";
        using UnityWebRequest www = UnityWebRequest.Get(url);
        var op = www.SendWebRequest();
        while (!op.isDone) { }
        if (www.result == UnityWebRequest.Result.Success)
            importAction(www.downloadHandler.text);
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
}