using System;
using System.Collections.Generic;
using UnityEngine;

public enum EventKind { Reward, Shop, Encounter, Quest }
public enum EventActionKind { None, AcceptQuest, CompleteQuest, Combat }

[CreateAssetMenu(menuName = "Events/Event Definition", fileName = "EventDefinition")]
public sealed class EventDefinition : ScriptableObject
{
    public EventKind kind;
    public string title;
    public Node exitNode;
    public List<EventOption> options = new List<EventOption>();
}

[Serializable]
public sealed class EventOption
{
    public string id;
    public string text;
    [Min(0)] public int goldCost;
    [Min(0)] public int goldLoss;
    public bool repeatable;
    public string requiredQuest;
    public BaseItem requiredItem;
    [Min(1)] public int requiredQuantity = 1;
    public EventActionKind action;
    public string questId;
    public Reward reward;
    // 전투 선택지는 이 노드로 이동한다. 공유 CombatNode의 성공 노드는 수정하지 않는다.
    public Node nextNode;
}
