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
    public string ResultText;
    // 빈 셀을 0%와 구별해 기본 100%로 처리한다.
    public string SuccessProbability = "1";
    public string FailureText;
    public int GoldCost, GoldLoss;
    public bool Repeatable;
    public string RequiredQuestID, RequiredItemID;
    public int RequiredQuantity;
    public string Action, QuestID, NextNodeID, RewardID;
    public string RequiredQuestState = "Active";
    public string TendencyCondition = "Any";
    public int TendencyMin, TendencyMax, RequiredGold, RequiredSTR, RequiredDEX, RequiredCON;
}

[Serializable]
public sealed class RewardDataRaw
{
    public string RewardID, EntryID, Kind, ItemID;
    public int Amount;
    public float Probability;
}
