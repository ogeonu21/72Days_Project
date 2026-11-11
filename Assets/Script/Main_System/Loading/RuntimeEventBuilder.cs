using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Editor 임포터와 같은 이벤트 계약. 런타임은 검증된 임시 정의만 게시한다.
public static class RuntimeEventBuilder
{
    public static Dictionary<string, EventDefinition> BuildEventDefinitions(string events, string choices, string rewards,
        Func<string, BaseItem> findItem, Func<string, Node> findNode)
    {
        var eventRows = SheetJson.Read<EventDataRaw>(events, true);
        var choiceRows = SheetJson.Read<EventChoiceDataRaw>(choices, true);
        var rewardRows = SheetJson.Read<RewardDataRaw>(rewards, true);
        var result = new Dictionary<string, EventDefinition>(StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var choiceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rewardMap = new Dictionary<string, Reward>(StringComparer.Ordinal);
        try
        {
            foreach (var row in rewardRows)
            {
                CheckId(row.RewardID, new HashSet<string>(), "RewardData RewardID");
                CheckId(row.EntryID, new HashSet<string>(), "RewardData EntryID");
                if (!entryIds.Add(row.RewardID + "/" + row.EntryID)) throw new InvalidOperationException(row.RewardID + ": 보상 EntryID 중복");
                if (row.Kind == "Tendency" ? row.Amount == 0 : row.Amount < 1) throw new InvalidOperationException(row.RewardID + ": Amount는 1 이상, Tendency는 0이 아닌 정수여야 합니다.");
                CheckRate(row.Probability, row.RewardID + " Probability");
                if (!rewardMap.TryGetValue(row.RewardID, out var reward)) reward = new Reward { items = new List<ItemReward>() };
                if (row.Kind == "Item")
                {
                    BaseItem item = string.IsNullOrWhiteSpace(row.ItemID) ? null : findItem(row.ItemID);
                    if (item == null) throw new InvalidOperationException(row.RewardID + ": 아이템 누락 " + row.ItemID);
                    reward.items.Add(new ItemReward { item = item, quantity = row.Amount, probability = row.Probability });
                }
                else
                {
                    if (row.Probability != 1 || !string.IsNullOrWhiteSpace(row.ItemID)) throw new InvalidOperationException(row.RewardID + ": 비아이템 보상은 Probability=1, ItemID는 빈 값이어야 합니다.");
                    checked
                    {
                        switch (row.Kind)
                        {
                            case "Gold": reward.dropGold += row.Amount; break;
                            case "Heal": reward.hpHeal += row.Amount; break;
                            case "Exp": reward.exp += row.Amount; break;
                            case "STR": reward.statIncrease.str += row.Amount; break;
                            case "DEX": reward.statIncrease.dex += row.Amount; break;
                            case "CON": reward.statIncrease.con += row.Amount; break;
                            case "Tendency": reward.tendencyChange += row.Amount; break;
                            default: throw new InvalidOperationException(row.RewardID + ": 보상 종류 오류 " + row.Kind);
                        }
                    }
                }
                string error = RewardService.Validate(reward);
                if (error != null) throw new InvalidOperationException(row.RewardID + ": " + error);
                rewardMap[row.RewardID] = reward;
            }
            foreach (var row in eventRows)
            {
                CheckId(row.EventID, ids, "EventData");

                var definition = ScriptableObject.CreateInstance<EventDefinition>();
                result.Add(row.EventID, definition);
                definition.name = row.EventID;
                definition.kind = NamedEnum<EventKind>(row.Kind, row.EventID);
                if (string.IsNullOrWhiteSpace(row.Title)) throw new InvalidOperationException(row.EventID + ": Title 필수");
                definition.title = SheetJson.Multiline(row.Title);
                definition.exitNode = string.IsNullOrWhiteSpace(row.ExitNodeID) ? null : findNode(row.ExitNodeID);
            }
            foreach (var row in choiceRows.OrderBy(c => c.SortOrder))
            {
                if (!result.TryGetValue(row.EventID, out var definition)) throw new InvalidOperationException(row.EventID + ": 선택지의 이벤트 누락");
                CheckId(row.ChoiceID, new HashSet<string>(), "ChoiceID");
                if (!choiceIds.Add(row.EventID + "/" + row.ChoiceID)) throw new InvalidOperationException(row.EventID + ": ChoiceID 중복");
                if (row.SortOrder < 0) throw new InvalidOperationException(row.EventID + ": SortOrder는 0 이상");
                var option = new EventOption
                {
                    id = row.ChoiceID, text = SheetJson.Multiline(row.Text), goldCost = row.GoldCost, goldLoss = row.GoldLoss,
                    resultText = SheetJson.Multiline(row.ResultText),
                    successProbability = ParseEventProbability(row.SuccessProbability, row.ChoiceID),
                    failureText = SheetJson.Multiline(row.FailureText),
                    repeatable = row.Repeatable, requiredQuest = row.RequiredQuestID,
                    requiredQuantity = row.RequiredQuantity, action = NamedEnum<EventActionKind>(row.Action, row.ChoiceID), questId = row.QuestID
                };
                option.requiredQuestState = NamedEnum<QuestRequirementState>(string.IsNullOrWhiteSpace(row.RequiredQuestState) ? "Active" : row.RequiredQuestState, row.ChoiceID);
                option.tendencyCondition = NamedEnum<TendencyRequirement>(string.IsNullOrWhiteSpace(row.TendencyCondition) ? "Any" : row.TendencyCondition, row.ChoiceID);
                option.tendencyMin = row.TendencyMin; option.tendencyMax = row.TendencyMax;
                option.requiredGold = row.RequiredGold;
                option.requiredStats = new BaseStats(row.RequiredSTR, row.RequiredDEX, row.RequiredCON);
                if (!string.IsNullOrWhiteSpace(row.RequiredItemID))
                {
                    option.requiredItem = findItem(row.RequiredItemID);
                    if (option.requiredItem == null) throw new InvalidOperationException(row.ChoiceID + ": 조건 아이템 누락");
                }
                else if (row.RequiredQuantity != 0) throw new InvalidOperationException(row.ChoiceID + ": RequiredItemID 없이 수량만 입력됨");
                if (!string.IsNullOrWhiteSpace(row.NextNodeID))
                {
                    option.nextNode = findNode(row.NextNodeID);
                    if (option.nextNode == null) throw new InvalidOperationException(row.ChoiceID + ": 다음 노드 누락");
                }
                if (!string.IsNullOrWhiteSpace(row.RewardID))
                {
                    if (!rewardMap.TryGetValue(row.RewardID, out option.reward)) throw new InvalidOperationException(row.ChoiceID + ": RewardID 누락");
                }
                definition.options.Add(option);
            }
            var acceptedQuestIds = new HashSet<string>(choiceRows.Where(c => c.Action == "AcceptQuest").Select(c => c.QuestID), StringComparer.Ordinal);
            foreach (var row in choiceRows)
            {
                if (row.Action == "CompleteQuest" && !acceptedQuestIds.Contains(row.QuestID)) throw new InvalidOperationException(row.ChoiceID + ": 수락 선택지가 없는 퀘스트 " + row.QuestID);
                if (!string.IsNullOrWhiteSpace(row.RequiredQuestID) && !acceptedQuestIds.Contains(row.RequiredQuestID)) throw new InvalidOperationException(row.ChoiceID + ": 조건 퀘스트 수락 선택지 누락 " + row.RequiredQuestID);
            }
            foreach (var pair in result)
            {
                string error = EventDefinitionValidator.Validate(pair.Value);
                if (error != null) throw new InvalidOperationException(pair.Key + ": " + error);
            }
            return result;
        }
        catch { foreach (var obj in result.Values) UnityEngine.Object.DestroyImmediate(obj); throw; }
    }

    private static float ParseEventProbability(string value, string id)
    {
        if (string.IsNullOrWhiteSpace(value)) return 1f;
        if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float probability))
            throw new InvalidOperationException(id + ": SuccessProbability는 0~1 숫자여야 합니다.");
        CheckRate(probability, id + " SuccessProbability");
        return probability;
    }


    private static void CheckId(string id, HashSet<string> ids, string context) { RuntimeStoryCatalog.CheckId(id); if (!ids.Add(id)) throw new InvalidOperationException(context + ": 중복 ID " + id); }
    private static void CheckRate(float rate, string context) { if (float.IsNaN(rate) || float.IsInfinity(rate) || rate<0 || rate>1) throw new InvalidOperationException(context + ": 확률 오류"); }
    private static T NamedEnum<T>(string value, string context) where T:struct { if (!Enum.TryParse(value, out T parsed) || !Enum.IsDefined(typeof(T), parsed) || parsed.ToString()!=value) throw new InvalidOperationException(context + ": 유형 오류"); return parsed; }
}
