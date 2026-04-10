using UnityEngine;
using UnityEngine.Android;

[System.Serializable]
public class NodeDataRaw
{
    public string NodeType, NodeID, NodeMessage, WorldLocation;
    public int SurviveDate;
    
    public string NextNode;
    //전투관련 NodeData
    public string SuccessNode, FailureNode, CombatEnemyID;
    public string EventCategory, EndingName;
    public string Choice1_Text, Choice1_NextNode, Choice1_EventName;
    public string Choice2_Text, Choice2_NextNode, Choice2_EventName;
    public string Choice3_Text, Choice3_NextNode, Choice3_EventName;
}