using System;

[Serializable]
public sealed class EventDataRaw
{
    public string EventID, Kind, Title, ExitNodeID;
}

[Serializable]
public sealed class EventChoiceDataRaw
{
    public string EventID, ChoiceID;
    public int SortOrder;
    public string Text;
    public int GoldCost, GoldLoss;
    public bool Repeatable;
    public string RequiredQuestID, RequiredItemID;
    public int RequiredQuantity;
    public string Action, QuestID, NextNodeID, RewardID;
}

[Serializable]
public sealed class RewardDataRaw
{
    public string RewardID, EntryID, Kind, ItemID;
    public int Amount;
    public float Probability;
}
