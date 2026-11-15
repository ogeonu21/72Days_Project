using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public sealed class RuntimeStoryCatalog : IDisposable
{
    public static RuntimeStoryCatalog Active { get; private set; }
    public string StoryVersion { get; private set; }
    public string StartNodeId { get; private set; }
    private readonly Dictionary<string, Node> nodes = new Dictionary<string, Node>(StringComparer.Ordinal);
    private Dictionary<string, EventDefinition> events = new Dictionary<string, EventDefinition>();
    public int NodeCount => nodes.Count;
    public static Node Resolve(string id)
    { if(string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id,@"^[A-Za-z0-9_-]+$")) return null; return Active != null ? (Active.nodes.TryGetValue(id,out Node node) ? node : null) : Resources.Load<Node>("Nodes/" + id); }
    public static string Version => Active != null ? Active.StoryVersion : "";
    private Node Find(string id) { CheckId(id); if (!nodes.TryGetValue(id, out Node node)) throw new InvalidOperationException("노드 참조 누락: " + id); return node; }
    public static void CheckId(string id)
    { if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || !Regex.IsMatch(id, @"^[A-Za-z0-9_-]+$")) throw new InvalidOperationException("데이터 ID 형식 오류"); }
    public static bool Compatible(string saved, string current) => !string.IsNullOrWhiteSpace(saved) && string.Equals(saved, current, StringComparison.Ordinal);

    public static RuntimeStoryCatalog Build(string version, string startId, string nodeJson, string eventJson, string choiceJson, string rewardJson)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Length > 80) throw new InvalidOperationException("storyVersion 누락 또는 길이 초과");
        var catalog = new RuntimeStoryCatalog { StoryVersion = version, StartNodeId = startId };
        try
        {
            var rows = SheetJson.Read<NodeDataRaw>(nodeJson, true);
            if (rows.Length == 0 || rows.Length > 5000) throw new InvalidOperationException("노드 개수 오류");
            foreach (var row in rows)
            {
                CheckId(row.NodeID);
                if (!Enum.TryParse(row.NodeType, out NodeType type) || !Enum.IsDefined(typeof(NodeType), type) || type.ToString() != row.NodeType)
                    throw new InvalidOperationException(row.NodeID + ": 노드 종류 오류");
                Node node;
                switch(type) {
                    case NodeType.MainStoryNode: node=ScriptableObject.CreateInstance<MainStoryNode>(); break;
                    case NodeType.StoryNode: node=ScriptableObject.CreateInstance<StoryNode>(); break;
                    case NodeType.CombatNode: node=ScriptableObject.CreateInstance<CombatNode>(); break;
                    case NodeType.EventNode: node=ScriptableObject.CreateInstance<EventNode>(); break;
                    default: node=ScriptableObject.CreateInstance<EndingNode>(); break;
                }
                if(catalog.nodes.ContainsKey(row.NodeID)) { UnityEngine.Object.DestroyImmediate(node); throw new InvalidOperationException(row.NodeID+": 노드 ID 중복"); }
                catalog.nodes.Add(row.NodeID,node);
                node.name=row.NodeID; node.nodeName=row.NodeID; node.nodeType=type;
                node.nodeMessage=SheetJson.Multiline(row.NodeMessage); node.surviveDate=row.SurviveDate;
                if(row.SurviveDate<0 || !Enum.TryParse(row.WorldLocation,out WorldLocation location) || !Enum.IsDefined(typeof(WorldLocation),location)) throw new InvalidOperationException(row.NodeID+": 날짜/지역 오류");
                node.worldLocation=location;
            }
            catalog.events=RuntimeEventBuilder.BuildEventDefinitions(eventJson,choiceJson,rewardJson,
                id => { CheckId(id); return Resources.Load<BaseItem>("Items/"+id); },catalog.Find);
            foreach(var row in rows)
            {
                var node=catalog.Find(row.NodeID);
                if(node is MainStoryNode main) main.nextNode=catalog.Find(row.NextNode);
                else if(node is StoryNode story) {
                    story.choices=new List<Choice>();
                    string[] texts={row.Choice1_Text,row.Choice2_Text,row.Choice3_Text}; string[] links={row.Choice1_NextNode,row.Choice2_NextNode,row.Choice3_NextNode};
                    for(int i=0;i<3;i++) { if(!string.IsNullOrWhiteSpace(texts[i])) story.choices.Add(new Choice{choiceText=SheetJson.Multiline(texts[i]),nextNode=catalog.Find(links[i])}); else if(!string.IsNullOrWhiteSpace(links[i])) throw new InvalidOperationException(row.NodeID+": 선택지 문구 누락"); }
                    if(story.choices.Count==0) throw new InvalidOperationException(row.NodeID+": 선택지 없음");
                }
                else if(node is CombatNode combat) {
                    CheckId(row.CombatEnemyID); combat.combatEnemyID=row.CombatEnemyID;
                    combat.enemyData=Resources.Load<EnemyData>("Characters/"+row.CombatEnemyID);
                    if(combat.enemyData==null) throw new InvalidOperationException(row.NodeID+": 적 정의 누락");
                    combat.successNode=catalog.Find(row.SuccessNode); combat.failureNode=catalog.Find(row.FailureNode);
                }
                else if(node is EventNode evt) { if(!catalog.events.TryGetValue(row.EventDefinitionID??"",out evt.definition)) throw new InvalidOperationException(row.NodeID+": 이벤트 정의 누락"); }
                else if(node is EndingNode ending) ending.endingName=row.EndingName;
            }
            catalog.Find(startId);
            return catalog;
        }
        catch { catalog.Dispose(); throw; }
    }
    public static void Publish(RuntimeStoryCatalog catalog)
    { if(catalog==null) throw new ArgumentNullException(nameof(catalog)); var old=Active; Active=catalog; old?.Dispose(); }
    public void Dispose()
    { foreach(var node in nodes.Values) if(node!=null) UnityEngine.Object.DestroyImmediate(node); foreach(var evt in events.Values) if(evt!=null) UnityEngine.Object.DestroyImmediate(evt); nodes.Clear(); events.Clear(); }
}
