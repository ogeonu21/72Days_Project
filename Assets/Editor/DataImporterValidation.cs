using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class DataImporterValidation
{
    [MenuItem("Tools/Validation/Data Importer")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        int passed = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new InvalidOperationException(label); passed++; };
        Action<Action, string> reject = (action, label) =>
        {
            bool failed = false;
            try { action(); } catch (Exception) { failed = true; }
            check(failed, label);
        };
        var parsed = SheetJson.Read<StatusDataRaw>("[{\"ID\":\"Enemy\",\"DodgeBonus\":0.07,\"DropGold\":\"\"}]")[0];
        check(Math.Abs(parsed.DodgeBonus - 0.07f) < 0.00001f && parsed.DropGold == 0, "소수 회피율 및 빈 숫자");
        check(SheetJson.Read<ItemDataRaw>("[{\"Consumable\":\"TRUE\"}]")[0].Consumable, "시트 TRUE 문자열");
        reject(() => SheetJson.Read<ItemDataRaw>("[{\"Health\":1.5}]"), "정수 소수 잘림 거부");
        reject(() => SheetJson.Read<ItemDataRaw>("[{\"Consumable\":\"yes\"}]"), "불리언 오타 거부");
        reject(() => SheetJson.Read<StatusDataRaw>("[{\"DodgeBonus\":\"NaN\"}]"), "비정상 실수 거부");
        reject(() => SheetJson.Read<EventDataRaw>("{\"error\":\"시트 없음\"}"), "서버 오류 응답 거부");
        reject(() => SheetJson.Read<EventDataRaw>("[{\"EventID\":\"A\"}]", true), "필수 열 누락 거부");
        check(SheetJson.Multiline(@"앞\\\n뒤") == "앞\n뒤", "줄바꿈 복원");

        string prefix = "ImportCheck_" + Guid.NewGuid().ToString("N");
        var items = new[]
        {
            new ItemDataRaw { ItemID = prefix + "Weapon", ItemCategory = "Weapon", ItemName = "검사 무기", ItemDesc = @"첫줄\n둘째줄", ItemIcon = "test-address", ItemValue = 25, Durability = 8, AttackBonus = 4, Range = 2 },
            new ItemDataRaw { ItemID = prefix + "Armor", ItemCategory = "Armor", ArmorType = "Gloves", Durability = 7, HpBonus = 9, DodgeBonus = .05f },
            new ItemDataRaw { ItemID = prefix + "Accessory", ItemCategory = "Accessory", Durability = 8, DodgeBonus = .1f, QuestID = "Q_TEST" },
            new ItemDataRaw { ItemID = prefix + "Potion", ItemCategory = "Potion", Consumable = true, Health = 25 }
        };
        var stats = new[] { new StatusDataRaw { ID = prefix + "Enemy", Name = "검사 적", Type = "악", STR = 1, DEX = 2, CON = 3, AttackBonus = 4, HpBonus = 5, DodgeBonus = .07f, RangeBonus = 2, DropItemCategory = "Weapon", DropItemID = items[0].ItemID, ItemDropRate = .3f, DropGold = 320 } };
        var nodes = new[]
        {
            new NodeDataRaw { NodeID = prefix + "Exit", NodeType = "EndingNode", WorldLocation = "서울", EndingName = "검사 끝", SurviveDate = 1 },
            new NodeDataRaw { NodeID = prefix + "Event", NodeType = "EventNode", WorldLocation = "서울", EventDefinitionID = prefix + "Definition", NodeMessage = @"본문\n다음줄" },
            new NodeDataRaw { NodeID = prefix + "Combat", NodeType = "CombatNode", WorldLocation = "서울", CombatEnemyID = stats[0].ID, SuccessNode = prefix + "Exit", FailureNode = prefix + "Exit" }
        };
        var events = new[] { new EventDataRaw { EventID = prefix + "Definition", Kind = "Shop", Title = "검사 상점", ExitNodeID = nodes[0].NodeID } };
        var choices = new[]
        {
            new EventChoiceDataRaw { EventID = events[0].EventID, ChoiceID = "buy", SortOrder = 2, Text = "구매", GoldCost = 10, Repeatable = true, Action = "None", RewardID = prefix + "Reward" },
            new EventChoiceDataRaw { EventID = events[0].EventID, ChoiceID = "fight", SortOrder = 1, Text = "전투", Action = "Combat", NextNodeID = nodes[2].NodeID }
        };
        var rewards = new[]
        {
            new RewardDataRaw { RewardID = prefix + "Reward", EntryID = "item", Kind = "Item", ItemID = items[3].ItemID, Amount = 3, Probability = .5f },
            new RewardDataRaw { RewardID = prefix + "Reward", EntryID = "gold", Kind = "Gold", Amount = 20, Probability = 1 },
            new RewardDataRaw { RewardID = prefix + "Reward", EntryID = "str", Kind = "STR", Amount = 1, Probability = 1 },
            new RewardDataRaw { RewardID = prefix + "Reward", EntryID = "heal", Kind = "Heal", Amount = 5, Probability = 1 },
            new RewardDataRaw { RewardID = prefix + "Reward", EntryID = "exp", Kind = "Exp", Amount = 10, Probability = 1 }
        };
        Func<object, string> json = value => JsonConvert.SerializeObject(value);
        Action validate = () => DataImporter.ValidateSnapshot(json(items), json(stats), json(nodes), json(events), json(choices), json(rewards));
        validate(); check(true, "미생성 참조 포함 전체 사전 검사");
        choices[0].RewardID = "missing";
        reject(validate, "보상 ID 누락 거부"); choices[0].RewardID = prefix + "Reward";
        choices[1].NextNodeID = nodes[0].NodeID;
        reject(validate, "Combat의 비전투 노드 거부"); choices[1].NextNodeID = nodes[2].NodeID;
        rewards[1].Probability = .5f;
        reject(validate, "미지원 골드 확률 거부"); rewards[1].Probability = 1;
        choices[1].ChoiceID = "buy";
        reject(validate, "선택지 중복 거부"); choices[1].ChoiceID = "fight";
        stats[0].DropItemCategory = "Armor";
        reject(validate, "드롭 종류 불일치 거부"); stats[0].DropItemCategory = "Weapon";
        items[0].Consumable = true;
        reject(validate, "장비 Consumable 불일치 거부"); items[0].Consumable = false;
        nodes[1].EventDefinitionID = "";
        reject(validate, "구형 이벤트 노드 거부"); nodes[1].EventDefinitionID = events[0].EventID;
        choices[0].TendencyCondition = "Between"; choices[0].TendencyMin = 2; choices[0].TendencyMax = 1;
        reject(validate, "역전 성향 조건 거부"); choices[0].TendencyMin = -1; choices[0].TendencyMax = 3;
        choices[0].RequiredGold = -1;
        reject(validate, "음수 조건 골드 거부"); choices[0].RequiredGold = 50;
        choices[0].RequiredSTR = 1; choices[0].RequiredDEX = 2; choices[0].RequiredCON = 3;
        choices[0].RequiredItemID = items[3].ItemID; choices[0].RequiredQuantity = 2;
        choices[0].RequiredQuestID = "MissingQuest";
        reject(validate, "수락 경로 없는 조건 퀘스트 거부"); choices[0].RequiredQuestID = "";
        rewards[1].Kind = "Tendency"; rewards[1].Amount = -2;
        validate(); check(true, "음수 성향 보상 허용"); rewards[1].Kind = "Gold"; rewards[1].Amount = 20;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        var paths = new List<string>();
        foreach (var item in items) paths.Add("Assets/Resources/Items/" + item.ItemID + ".asset");
        paths.Add("Assets/Resources/Characters/" + stats[0].ID + ".asset");
        foreach (var node in nodes) paths.Add("Assets/Resources/Nodes/" + node.NodeID + ".asset");
        paths.Add("Assets/Resources/EventDefinitions/" + events[0].EventID + ".asset");
        try
        {
            DataImporter.ApplySnapshot(json(items), json(stats), json(nodes), json(events), json(choices), json(rewards));
            var weapon = Resources.Load<WeaponItem>("Items/" + items[0].ItemID);
            check(weapon.durability == 8 && weapon.attackBonus == 4 && weapon.range == 2 && weapon.itemValue == 25 && weapon.itemName == "검사 무기" && weapon.itemDescription.Contains("\n") && weapon.itemIcon == "test-address", "무기 공통/전용 필드 매핑");
            var armor = Resources.Load<ArmorItem>("Items/" + items[1].ItemID);
            check(armor.armorType == ArmorType.Gloves && armor.durability == 7 && armor.hpBonus == 9 && armor.dodgeBonus == .05f, "방어구 매핑");
            var accessory = Resources.Load<AccessoryItem>("Items/" + items[2].ItemID);
            check(accessory.durability == 8 && accessory.dodgeBonus == .1f && accessory.questID == "Q_TEST", "액세서리 내구도 포함 매핑");
            var potion = Resources.Load<PotionItem>("Items/" + items[3].ItemID);
            check(potion.health == 25 && potion.isConsumable, "물약 매핑");
            var enemy = Resources.Load<EnemyData>("Characters/" + stats[0].ID);
            check(enemy.tuningStats.dodgeBonus == .07f && enemy.baseStats.dex == 2 && enemy.tuningStats.rangeBonus == 2 && enemy.dropItem == weapon && enemy.itemDropRate == .3f && enemy.dropGold == 320, "적 능력치 및 드롭 매핑");
            var nodeAsset = Resources.Load<EventNode>("Nodes/" + nodes[1].NodeID);
            var definition = nodeAsset.definition;
            check(definition != null && definition.kind == EventKind.Shop && definition.title == "검사 상점" && definition.exitNode != null && nodeAsset.nodeMessage.Contains("\n"), "노드 및 EventDefinition 자동 연결");
            check(definition.options[0].id == "fight" && definition.options[0].nextNode is CombatNode, "선택지 정렬과 전투 연결");
            var reward = definition.options[1].reward;
            var mapped = definition.options[1];
            check(mapped.requiredGold == 50 && mapped.requiredStats.str == 1 && mapped.requiredStats.dex == 2 && mapped.requiredStats.con == 3 &&
                mapped.tendencyCondition == TendencyRequirement.Between && mapped.tendencyMin == -1 && mapped.tendencyMax == 3 &&
                mapped.requiredItem == potion && mapped.requiredQuantity == 2 && mapped.requiredQuestState == QuestRequirementState.Active, "신규 조건 전체 매핑");
            check(reward.items[0].item == potion && reward.items[0].quantity == 3 && reward.items[0].probability == .5f && reward.dropGold == 20 && reward.statIncrease.str == 1 && reward.hpHeal == 5 && reward.exp == 10, "복합 보상 매핑");
            string path = AssetDatabase.GetAssetPath(definition);
            string guid = AssetDatabase.AssetPathToGUID(path);
            events[0].Title = "갱신 상점"; items[2].Durability = 11;
            DataImporter.ApplySnapshot(json(items), json(stats), json(nodes), json(events), json(choices), json(rewards));
            check(AssetDatabase.AssetPathToGUID(path) == guid && nodeAsset.definition == definition && definition.title == "갱신 상점" && accessory.durability == 11, "재임포트 GUID/참조 유지 및 값 갱신");
        }
        finally
        {
            Undo.RevertAllDownToGroup(group);
            // Undo는 객체만 제거하고 .asset 파일을 남길 수 있으므로 이 검사에서 만든 경로만 정리한다.
            foreach (var path in paths) if (System.IO.File.Exists(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
        }
        foreach (var path in paths) check(!System.IO.File.Exists(path) && !System.IO.File.Exists(path + ".meta"), "임시 에셋 정리: " + path);
        return "Data Importer " + passed + "개 통과 (임시 에셋 원복, 실제 시트/세이브 변경 없음)";
    }
}
