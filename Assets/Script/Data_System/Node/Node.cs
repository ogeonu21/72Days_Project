using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WorldLocation
{
    서울,
    인천,
    강원도,
    경기도
}

public enum NodeType
{
    MainStoryNode,
    StoryNode,
    CombatNode,
    EventNode,
    EndingNode
}
[System.Serializable]
public class Node : ScriptableObject
{
    [Header("노드 기본 정보")]
    public NodeType nodeType;
    public string nodeName;

    [TextArea]
    public string nodeMessage;

    [Header("생존 날짜 및 위치")]
    public int surviveDate;
    public WorldLocation worldLocation;

    public NodeType GetCurrentNodeType()
    {
        return nodeType;
    }
}

[System.Serializable]
[CreateAssetMenu(fileName = "MainStoryNode", menuName = "Node/MainStoryNode", order = 0)]
public class MainStoryNode : Node
{
    public Node nextNode;
}

[System.Serializable]
[CreateAssetMenu(fileName = "StoryNode", menuName = "Node/StoryNode", order = 1)]
public class StoryNode : Node
{
    public List<Choice> choices = new List<Choice>();
}

[System.Serializable]
[CreateAssetMenu(fileName = "CombatNode", menuName = "Node/CombatNode", order = 2)]
public class CombatNode : Node
{
    public string combatEnemyID;
    public Node successNode;
    public Node failureNode;
}   

[System.Serializable]
[CreateAssetMenu(fileName = "EventNode", menuName = "Node/EventNode", order = 3)]
public class EventNode : Node
{
    public string eventName;
    public List<Choice> choices = new List<Choice>();
}

[System.Serializable]
[CreateAssetMenu(fileName = "EndingNode", menuName = "Node/EndingNode", order = 4)]
public class EndingNode : Node
{
    public string endingName;
}



[System.Serializable]
public class Choice
{
    public string choiceText;
    public Node nextNode;

    [Header("상태 플래그")]
    public string requiredFlag;
    //이벤트 발생 횟수, 발생 여부 등을 감지.
    public int eventStack;

    public bool triggersEvent;
    public float eventSuccessRate; // 이벤트 성공 확률

}