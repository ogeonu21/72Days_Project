using System;
using System.Collections.Generic;
using UnityEngine;

public enum EventKind { Reward, Shop, Encounter, Quest }
public enum EventActionKind { None, AcceptQuest, CompleteQuest, Combat }
public enum QuestRequirementState { Active, Completed, NotAccepted, Accepted }
public enum TendencyRequirement { Any, AtLeast, AtMost, Between }

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
    [Tooltip("선택 전 버튼에 표시하는 문구. EventChoiceData.Text")]
    public string text;
    [Tooltip("실행 성공 후 dialogue에 표시하고 클릭을 기다리는 문구. EventChoiceData.ResultText")]
    [TextArea] public string resultText;
    [Range(0, 1)] public float successProbability = 1f;
    [TextArea] public string failureText;

    public bool RollSuccess(Func<float> roll = null)
    {
        if (successProbability >= 1f) return true;
        if (successProbability <= 0f) return false;
        return (roll != null ? roll() : UnityEngine.Random.value) < successProbability;
    }
    [Min(0)] public int goldCost;
    [Min(0)] public int goldLoss;
    public bool repeatable;
    public string requiredQuest;
    public QuestRequirementState requiredQuestState;
    public BaseItem requiredItem;
    [Min(1)] public int requiredQuantity = 1;
    [Min(0)] public int requiredGold;
    public BaseStats requiredStats;
    public TendencyRequirement tendencyCondition;
    public int tendencyMin;
    public int tendencyMax;
    public EventActionKind action;
    public string questId;
    public Reward reward;
    // 전투 선택지는 이 노드로 이동한다. 공유 CombatNode의 성공 노드는 수정하지 않는다.
    public Node nextNode;
}
