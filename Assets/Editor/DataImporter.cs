using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

public class DataImporter : EditorWindow
{
    
    private static TextAsset statsCSVFile;
    private static TextAsset nodeCSVFile;
    private static string path = "Assets/Resources/";
    private static string statsCSVFileName = "StatsData.csv";
    private static string nodeCSVFileName = "NodeData.csv";
    private static Dictionary<string, Node> nodeMap = new Dictionary<string, Node>();

    [MenuItem("Tools/Import CSV")]
    public static void ImportCSV()
    {
        // 1. path와 fileName 변수가 static이므로 접근 가능
        statsCSVFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine(path, statsCSVFileName));
        if (statsCSVFile != null)
        {
            ImportStatsCSV(statsCSVFile.text);
        }
        else
        {
            Debug.Log("스탯 CSV 파일이 없습니다. 이름이 " + statsCSVFileName + "인지 확인하세요.");
        }
        nodeCSVFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine(path, nodeCSVFileName));
        if (nodeCSVFile != null)
        {
            ImportNodeCSV(nodeCSVFile.text);
        }
        else
        {
            Debug.Log("노드 CSV 파일이 없습니다. 이름이 " + nodeCSVFileName + "인지 확인하세요.");
        }
    }


    #region [Stats Import]
    private static void ImportStatsCSV(string csv)
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

            if (string.IsNullOrWhiteSpace(id)) continue;

            // 안전하게 파싱
            // 문화권에 따른 소수점(닷/콤마) 문제를 해결하려면 InvariantCulture를 사용하는 것이 좋습니다.
            int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int str);
            int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int dex);
            int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int con);
            int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int attackBonus);
            int.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hpBonus);
            // float 파싱 시 CultureInfo.InvariantCulture를 적용하여 소수점 오류 방지
            float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out float dodgeBonus);
            int.TryParse(parts[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rangeBonus);
            #endregion

            #region [Update SO]
            string soPath = $"Assets/Resources/Characters/{id}.asset"; // 지역 변수 이름 변경 (멤버 path와의 혼동 방지)
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(soPath);
            bool created = false;
            if (def == null)
            {
                // EnemyDefinition, BaseStats, TuningStats 타입이 프로젝트에 정의되어 있어야 함.
                def = ScriptableObject.CreateInstance<EnemyDefinition>();
                created = true;
            }

            // def.baseStats 및 def.tuningStats에 대한 클래스 정의가 필요
            def.type = type;
            def.id = id;
            def.displayName = string.IsNullOrEmpty(name) ? id : name;
            def.baseStats = new BaseStats(str, dex, con);
            def.tuningStats = new TuningStats(attackBonus, hpBonus, dodgeBonus, rangeBonus);
            #endregion

            if (created) AssetDatabase.CreateAsset(def, soPath);
            else EditorUtility.SetDirty(def);

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CSV 데이터 import 완료.");
    }

    #endregion

    #region [Nodes Import]
    
    private static void ImportNodeCSV(string csv)
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

    private static void ParseNodes(string[] lines)
    {
        nodeMap.Clear(); // 맵 초기화

        // CSV 파일 구조를 가정하고 작성된 코드
        // lines[2], lines[3], lines[4], lines[5], lines[6]에 데이터가 있어야 함.
        string[] nodeTypeLine = lines[2].Split(',');
        string[] nodeNameLine = lines[3].Split(',');
        string[] nodeMessageLine = lines[4].Split(',');
        string[] surviveDateLine = lines[5].Split(',');
        string[] worldLocationLine = lines[6].Split(',');

        for (int i = 3; i < nodeNameLine.Length; i++)
        {
            // 인덱스 i가 배열의 범위를 벗어나는지 확인하는 방어 로직 추가
            if (i >= nodeTypeLine.Length || i >= nodeMessageLine.Length || i >= surviveDateLine.Length || i >= worldLocationLine.Length)
            {
                Debug.LogWarning($"노드 데이터 파싱 중 라인 인덱스가 부족합니다. 열: {i}");
                continue;
            }
            
            string nodeName = nodeNameLine[i].Trim();
            if (string.IsNullOrEmpty(nodeName)) continue;

            NodeType type;
            // Enum.TryParse 사용 시 성공 여부를 체크합니다.
            if (!System.Enum.TryParse(nodeTypeLine[i].Trim(), out type))
            {
                Debug.LogError($"알 수 없는 노드 타입입니다: {nodeTypeLine[i].Trim()} (노드 이름: {nodeName})");
                continue;
            }

            //노드 생성
            Node node = CreateNodeInstance(type);

            if (node == null)
            {
                Debug.LogError($"알 수 없는 노드 타입으로 인해 인스턴스 생성이 실패했습니다: {type} (노드 이름: {nodeName})");
                continue;
            }

            //노드 기본값 채우기
            node.nodeType = type;
            node.nodeName = nodeName;
            node.nodeMessage = nodeMessageLine[i].Trim();
            
            // surviveDate 파싱 실패를 처리합니다.
            if (!int.TryParse(surviveDateLine[i].Trim(), out node.surviveDate))
            {
                 Debug.LogWarning($"노드 {nodeName}의 surviveDate 파싱에 실패했습니다. 기본값 0 사용.");
            }
            
            // WorldLocation Enum 파싱 실패를 처리합니다.
            try
            {
                node.worldLocation = (WorldLocation)System.Enum.Parse(typeof(WorldLocation), worldLocationLine[i].Trim());
            }
            catch (System.ArgumentException)
            {
                Debug.LogError($"노드 {nodeName}의 WorldLocation 값이 유효하지 않습니다: {worldLocationLine[i].Trim()}");
                // 기본값 할당 등의 로직 추가 고려
            }

            string path = $"Assets/Resources/Nodes/{nodeName}.asset";
            AssetDatabase.CreateAsset(node, path);
            nodeMap[nodeName] = node;
        }
    }
    

    private static Node CreateNodeInstance(NodeType type)
    {
        // 모든 Node 파생 클래스(MainStoryNode, StoryNode, CombatNode, EventNode, EndingNode)가 ScriptableObject를 상속하고 정의되어 있어야 함.
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

    #region [Node Parsing2]


    private static void LinkMainStoryNode(MainStoryNode mainStoryNode, string[] lines, int col)
    {
        // lines[8]에 다음 노드 이름이 있다고 가정
        string[] nextNodeNameLine = lines.Length > 8 ? lines[8].Split(',') : null;
        if (nextNodeNameLine == null || nextNodeNameLine.Length <= col) return;

        string nextNodeName = nextNodeNameLine[col].Trim();
        if (nodeMap.ContainsKey(nextNodeName))
        {
            mainStoryNode.nextNode = nodeMap[nextNodeName];
            EditorUtility.SetDirty(mainStoryNode); // 변경 사항 저장
        }
    }
    
    private static void LinkStoryNode(StoryNode storyNode, string[] lines, int col)
    {
        int howManyChoices = 0;
        
        // lines[11]의 col에 선택지 개수가 있다고 가정
        if (lines.Length > 11 && lines[11].Split(',').Length > col && 
            !string.IsNullOrEmpty(lines[11].Split(',')[col].Trim()))
        {
            int.TryParse(lines[11].Split(',')[col].Trim(), out howManyChoices);
        }

        // 선택지 데이터가 6줄 간격으로 있다고 가정: 12, 13, 14, 15, 16, 17... (12 + j*6)
        for (int j = 0; j < howManyChoices; j++)
        {
            int choiceTextRow = 12 + (j * 6);
            int nextNodeRow = 13 + (j * 6);
            
            // 배열 범위 초과 방지
            if (choiceTextRow >= lines.Length || nextNodeRow >= lines.Length) break;
            
            string[] choiceTextParts = lines[choiceTextRow].Split(',');
            string[] nextNodeParts = lines[nextNodeRow].Split(',');
            
            if (choiceTextParts.Length <= col || nextNodeParts.Length <= col) continue;

            Choice choice = new Choice();
            choice.choiceText = choiceTextParts[col].Trim();
            
            string nextNodeName = nextNodeParts[col].Trim();
            if (nodeMap.ContainsKey(nextNodeName))
            {
                choice.nextNode = nodeMap[nextNodeName];
            }
            storyNode.choices.Add(choice);
        }
        EditorUtility.SetDirty(storyNode); // 변경 사항 저장
    }
    
    private static void LinkCombatNode(CombatNode combatNode, string[] lines, int col)
    {
        // lines[31], lines[32], lines[33]에 데이터가 있다고 가정
        if (lines.Length <= 33) return;
        
        // lines[31] - combatEnemyID
        string[] enemyIDParts = lines[31].Split(',');
        if (enemyIDParts.Length > col)
        {
            combatNode.combatEnemyID = enemyIDParts[col].Trim();
            combatNode.enemyData = Resources.Load<EnemyDefinition>($"Characters/{combatNode.combatEnemyID}");
    
            if (combatNode.enemyData == null)
            {
                Debug.LogWarning($"비상비상비상비상비상비상 {combatNode.nodeName}에서 EnemyDefinition(ID: {combatNode.combatEnemyID})를 못찾음 비상비상비상!!!!");
            }
        }

        // lines[32] - successNode
        string[] successNodeParts = lines[32].Split(',');
        if (successNodeParts.Length > col)
        {
            string successNodeName = successNodeParts[col].Trim();
            if (nodeMap.ContainsKey(successNodeName))
            {
                combatNode.successNode = nodeMap[successNodeName];
            }
        }
        
        // lines[33] - failureNode
        string[] failureNodeParts = lines[33].Split(',');
        if (failureNodeParts.Length > col)
        {
            string failureNodeName = failureNodeParts[col].Trim();
            if (nodeMap.ContainsKey(failureNodeName))
            {
                combatNode.failureNode = nodeMap[failureNodeName];
            }
        }

        EditorUtility.SetDirty(combatNode); // 변경 사항 저장
    }
    
    private static void LinkEventNode(EventNode eventNode, string[] lines, int col)
    {
        // lines[35]에 이벤트 카테고리
        if (lines.Length <= 41) return;
        
        string[] eventCatParts = lines[35].Split(',');
        if (eventCatParts.Length > col)
        {
            eventNode.eventCategory = eventCatParts[col].Trim();
        }
        
        EditorUtility.SetDirty(eventNode); // 변경 사항 저장

        int howManyChoices = 0;
        
        // lines[37]에 선택지 개수
        if (lines.Length > 37 && lines[37].Split(',').Length > col && 
            !string.IsNullOrEmpty(lines[37].Split(',')[col].Trim()))
        {
            int.TryParse(lines[37].Split(',')[col].Trim(), out howManyChoices);
        }

        // 선택지 데이터가 6줄 간격으로 있다고 가정: 38, 39, 40, 41, 42, 43... (38 + j*6)
        for (int j = 0; j < howManyChoices; j++)
        {
            int choiceTextRow = 38 + (j * 6);
            int nextNodeRow = 39 + (j * 6);
            int eventNameRow = 41 + (j * 6);
            
            // 배열 범위 초과 방지
            if (choiceTextRow >= lines.Length || nextNodeRow >= lines.Length || eventNameRow >= lines.Length) break;
            
            string[] choiceTextParts = lines[choiceTextRow].Split(',');
            string[] nextNodeParts = lines[nextNodeRow].Split(',');
            string[] eventNameParts = lines[eventNameRow].Split(',');
            
            if (choiceTextParts.Length <= col || nextNodeParts.Length <= col || eventNameParts.Length <= col) continue;

            Choice choice = new Choice();
            choice.choiceText = choiceTextParts[col].Trim();
            
            string nextNodeName = nextNodeParts[col].Trim();
            if (nodeMap.ContainsKey(nextNodeName))
            {
                choice.nextNode = nodeMap[nextNodeName];
            }

            string eventName = eventNameParts[col].Trim();
            
            // Event 카테고리를 추가
            choice.baseEvent = Resources.Load<BaseEvent>($"Events/{eventNode.eventCategory}/{eventName}");
            
            if (choice.baseEvent == null && !string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning($"이벤트 노드 {eventNode.nodeName}의 선택지 {j+1}에서 이벤트 데이터(경로: Events/{eventNode.eventCategory}/{eventName})를 로드하지 못했습니다.");
            }

            eventNode.choices.Add(choice);
        }
        EditorUtility.SetDirty(eventNode); // 변경 사항 저장
    }
    
    private static void LinkEndingNode(EndingNode endingNode, string[] lines, int col)
    {
        // lines[57]에 엔딩 이름이 있다고 가정
        if (lines.Length <= 57) return;

        string[] endingNameParts = lines[57].Split(',');
        if (endingNameParts.Length > col)
        {
            endingNode.endingName = endingNameParts[col].Trim();
        }
        EditorUtility.SetDirty(endingNode); // 변경 사항 저장
    }
    #endregion

    #region [Node Link]
    private static void LinkNodes(string[] lines)
    {
        // lines[2], lines[3]에 노드 타입과 이름이 있다고 가정
        if (lines.Length < 4) return;
        
        string[] nodeTypeLine = lines[2].Split(',');
        string[] nodeNameLine = lines[3].Split(',') ;

        int nodeCount = nodeNameLine.Length;

        //노드의 개수만큼 한 번 돌기.
        for (int i = 3; i < nodeCount; i++) // CSV 열 인덱스 i (Column Index)
        {
            // 인덱스 i가 배열의 범위를 벗어나는지 확인하는 방어 로직 추가
            if (i >= nodeTypeLine.Length)
            {
                Debug.LogWarning($"LinkNodes 파싱 중 라인 인덱스가 부족합니다. 열: {i}");
                continue;
            }
            
            string nodeName = nodeNameLine[i].Trim();
            if (string.IsNullOrEmpty(nodeName)) continue;

            NodeType type;
            if (!System.Enum.TryParse(nodeTypeLine[i].Trim(), out type))
            {
                // ParseNodes에서 이미 로그를 남겼으므로 여기서는 로그를 생략하고 건너뜁니다.
                continue;
            }

            // ParseNodes에서 이미 생성하고 맵에 넣었는지 확인
            if (!nodeMap.ContainsKey(nodeName))
            {
                 Debug.LogError($"LinkNodes: 이전에 파싱된 노드 {nodeName}을 nodeMap에서 찾을 수 없습니다.");
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