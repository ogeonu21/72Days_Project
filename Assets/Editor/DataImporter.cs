using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

public class DataImporter : EditorWindow
{
    private TextAsset statsCSVFile;
    private TextAsset nodeCSVFile;

    private Dictionary<string, Node> nodeMap = new Dictionary<string, Node>();

    [MenuItem("Tools/Import CSV")]
    public static void ShowWindow()
    {
        GetWindow<DataImporter>("CSV Data Importer");
    }

    #region [GUI On]
    private void OnGUI()
    {
        GUILayout.Label("StatusData CSV 파일을 선택하세요", EditorStyles.boldLabel);
        statsCSVFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", statsCSVFile, typeof(TextAsset), false);

        if (GUILayout.Button("Import and Generate ScriptableObjects"))
        {
            if (statsCSVFile != null)
                ImportStatsCSV(statsCSVFile.text);
            else
                Debug.LogWarning("CSV 파일이 없습니다.");
        }

        GUILayout.Label("NodeData CSV 파일을 선택하세요", EditorStyles.boldLabel);
        nodeCSVFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", nodeCSVFile, typeof(TextAsset), false);

        if (GUILayout.Button("Import and Generate ScriptableObjects"))
        {
            if (nodeCSVFile != null)
                ImportNodeCSV(nodeCSVFile.text);
            else
                Debug.LogWarning("CSV 파일이 없습니다.");
        }
    }
    #endregion

    #region [Stats Import]
    private void ImportStatsCSV(string csv)
    {
        string[] lines = csv.Split('\n');

        for (int i = 3; i < lines.Length; i++) // 첫 줄과 둘째 줄은 제외
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(',');
            if (parts.Length < 10)
            {
                Debug.LogWarning($"라인 무시됨 (데이터 부족): {line}");
                continue;
            }

            #region [Data Parsing]
            string type = parts[0].Trim();
            string id = parts[1].Trim();
            string name = parts[2].Trim();

            // 안전하게 파싱
            int.TryParse(parts[3], out int str);
            int.TryParse(parts[4], out int dex);
            int.TryParse(parts[5], out int con);
            int.TryParse(parts[6], out int attackBonus);
            int.TryParse(parts[7], out int hpBonus);
            float.TryParse(parts[8], out float dodgeBonus);
            int.TryParse(parts[9], out int rangeBonus);

            #endregion

            if (type != "npc") continue;

            #region [Update SO]
            string path = $"Assets/Resources/NPCStats/{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            bool created = false;
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<EnemyDefinition>();
                created = true;
            }

            def.id = id;
            def.displayName = string.IsNullOrEmpty(name) ? id : name;
            def.baseStats = new BaseStats { str = str, dex = dex, con = con };
            def.attackBonus = attackBonus;
            def.hpBonus = hpBonus;
            def.dodgeBonus = Mathf.Clamp01(dodgeBonus); // 0~1 범위 권장
            def.rangeBonus = rangeBonus;
            #endregion

            if (created) AssetDatabase.CreateAsset(def, path);
            else EditorUtility.SetDirty(def);

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CSV 데이터 import 완료.");
    }

    #endregion

    #region [Nodes Import]
    private void ImportNodeCSV(string csv)
    {
        // 기존 노드 에셋 삭제
        string[] oldGuids = AssetDatabase.FindAssets("t:Node", new[] { "Assets/Resources/Nodes" });
        foreach (string guid in oldGuids)
        {
            string oldPath = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(oldPath);
        }

        string[] lines = csv.Split('\n');

        // 1. 모든 노드 데이터를 읽고 인스턴스 생성
        ParseNodes(lines);

        // 2. 노드들 간의 연결 설정
        LinkNodes(lines);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("노드 파싱 및 연결이 완료되었습니다. ");
    }

    #region [Node Parsing]
    private void ParseNodes(string[] lines)
    {
        nodeMap.Clear(); // 맵 초기화

        string[] nodeTypeLine = lines[2].Split(',');
        string[] nodeNameLine = lines[3].Split(',');
        string[] nodeMessageLine = lines[4].Split(',');
        string[] surviveDateLine = lines[5].Split(',');
        string[] worldLocationLine = lines[6].Split(',');

        for (int i = 3; i < nodeNameLine.Length; i++)
        {
            string nodeName = nodeNameLine[i].Trim();
            if (string.IsNullOrEmpty(nodeName)) continue;

            NodeType type;
            if (!System.Enum.TryParse(nodeTypeLine[i].Trim(), out type))
            {
                Debug.LogError($"알 수 없는 노드 타입입니다: {nodeTypeLine[i]}");
                continue;
            }

            //노드 생성
            Node node = CreateNodeInstance(type);

            //노드 기본값 채우기
            node.nodeType = type;
            node.nodeName = nodeName;
            node.nodeMessage = nodeMessageLine[i].Trim();
            int.TryParse(surviveDateLine[i].Trim(), out node.surviveDate);
            node.worldLocation = (WorldLocation)System.Enum.Parse(typeof(WorldLocation), worldLocationLine[i].Trim());

            string path = $"Assets/Resources/Nodes/{nodeName}.asset";
            AssetDatabase.CreateAsset(node, path);
            nodeMap[nodeName] = node;

        }
    }
    private Node CreateNodeInstance(NodeType type)
    {
        switch (type)
        {
            case NodeType.MainStoryNode: return ScriptableObject.CreateInstance<MainStoryNode>();
            case NodeType.StoryNode: return ScriptableObject.CreateInstance<StoryNode>();
            case NodeType.CombatNode: return ScriptableObject.CreateInstance<CombatNode>();
            case NodeType.EventNode: return ScriptableObject.CreateInstance<EventNode>();
            case NodeType.EndingNode: return ScriptableObject.CreateInstance<EndingNode>();
            default: return null;
        }
    }

    #endregion

    #region [Node Link2]

    private void LinkMainStoryNode(MainStoryNode mainStoryNode, string[] lines, int col)
    {
        string[] nextNodeNameLine = lines[8].Split(',');
        string nextNodeName = nextNodeNameLine[col].Trim();
        if (nodeMap.ContainsKey(nextNodeName))
        {
            mainStoryNode.nextNode = nodeMap[nextNodeName];
            EditorUtility.SetDirty(mainStoryNode); // 변경 사항 저장
        }
    }
    private void LinkStoryNode(StoryNode storyNode, string[] lines, int col)
    {
        int howManyChoices = 0;
        if (!string.IsNullOrEmpty(lines[11].Split(',')[col].Trim()))
        {
            int.TryParse(lines[11].Split(',')[col].Trim(), out howManyChoices);
        }

        for (int j = 0; j < howManyChoices; j++)
        {
            Choice choice = new Choice();
            choice.choiceText = lines[12 + (j * 6)].Split(',')[col].Trim();
            string nextNodeName = lines[13 + (j * 6)].Split(',')[col].Trim();
            if (nodeMap.ContainsKey(nextNodeName))
            {
                choice.nextNode = nodeMap[nextNodeName];
            }
            storyNode.choices.Add(choice);
        }
        EditorUtility.SetDirty(storyNode); // 변경 사항 저장
    }
    private void LinkCombatNode(CombatNode combatNode, string[] lines, int col)
    {
        combatNode.combatEnemyID = lines[31].Split(',')[col].Trim();

        string successNodeName = lines[32].Split(',')[col].Trim();
        if (nodeMap.ContainsKey(successNodeName))
        {
            combatNode.successNode = nodeMap[successNodeName];
        }

        string failureNodeName = lines[33].Split(',')[col].Trim();
        if (nodeMap.ContainsKey(failureNodeName))
        {
            combatNode.failureNode = nodeMap[failureNodeName];
        }

        EditorUtility.SetDirty(combatNode); // 변경 사항 저장
    }
    private void LinkEventNode(EventNode eventNode, string[] lines, int col)
    {
        eventNode.eventName = lines[35].Split(',')[col].Trim();
        EditorUtility.SetDirty(eventNode); // 변경 사항 저장

        int howManyChoices = 0;
        if (!string.IsNullOrEmpty(lines[37].Split(',')[col].Trim()))
        {
            int.TryParse(lines[37].Split(',')[col].Trim(), out howManyChoices);
        }

        for (int j = 0; j < howManyChoices; j++)
        {
            Choice choice = new Choice();
            choice.choiceText = lines[38 + (j * 6)].Split(',')[col].Trim();
            string nextNodeName = lines[39 + (j * 6)].Split(',')[col].Trim();
            if (nodeMap.ContainsKey(nextNodeName))
            {
                choice.nextNode = nodeMap[nextNodeName];
            }
            
            //BaseEvent는 직접 연결

            eventNode.choices.Add(choice);
        }
        EditorUtility.SetDirty(eventNode); // 변경 사항 저장
    }
    private void LinkEndingNode(EndingNode endingNode, string[] lines, int col)
    {
        endingNode.endingName = lines[57].Split(',')[col].Trim();
        EditorUtility.SetDirty(endingNode); // 변경 사항 저장
    }
    #endregion

    #region [Node Link]
    private void LinkNodes(string[] lines)
    {
        string[] nodeTypeLine = lines[2].Split(',');
        string[] nodeNameLine = lines[3].Split(',') ;

        int nodeCount = nodeNameLine.Length;

        //노드의 개수만큼 한 번 돌기.
        for (int i = 3; i < nodeCount; i++)
        {
            string nodeName = nodeNameLine[i].Trim();
            if (string.IsNullOrEmpty(nodeName)) continue;

            NodeType type;
            if (!System.Enum.TryParse(nodeTypeLine[i].Trim(), out type))
            {
                continue;
            }

            Node node = nodeMap[nodeName];

            switch (type)
            {
                case NodeType.MainStoryNode:
                    LinkMainStoryNode(node as MainStoryNode, lines, i);
                    break;
                case NodeType.StoryNode:
                    LinkStoryNode(node as StoryNode, lines, i);
                    break;
                case NodeType.CombatNode:
                    LinkCombatNode(node as CombatNode, lines, i);
                    break;
                case NodeType.EventNode:
                    LinkEventNode(node as EventNode, lines, i);
                    break;
                case NodeType.EndingNode:
                    LinkEndingNode(node as EndingNode, lines, i);
                    break;
            }
        }
    }
    #endregion

    #endregion
}