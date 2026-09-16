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

public class Node : ScriptableObject
{
    [Header("노드 기본 정보")]
    public NodeType nodeType;
    public string nodeName;

    [TextArea(3, 10)]
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
public class Choice
{
    public string choiceText;
    public Node nextNode;

    [Header("상태 플래그")]
    public string requiredFlag;
    //이벤트 발생 횟수, 발생 여부 등을 감지.
    public int eventStack;

    public bool triggersEvent;
    public BaseEvent baseEvent;

}