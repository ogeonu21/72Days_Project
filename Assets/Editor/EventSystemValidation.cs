using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EventSystemValidation
{
    [MenuItem("Tools/Validation/Event System")]
    public static void RunMenu() => Debug.Log(RunChecks());
    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new InvalidOperationException(name); passed++; };
        var root = new GameObject("EventValidation") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        var potion = ScriptableObject.CreateInstance<PotionItem>();
        var weapon = ScriptableObject.CreateInstance<WeaponItem>();
        var definition = ScriptableObject.CreateInstance<EventDefinition>();
        var exit = ScriptableObject.CreateInstance<StoryNode>();
        try
        {
            var player = root.AddComponent<Player>();
            player.InitializeFromData(new PlayerData());
            var inventory = root.AddComponent<InventoryManager>();
            var currency = root.AddComponent<CurrencyManager>();
            currency.currencyList.Add(new CurrencyData("Gold", 100));
            potion.itemID = "TestPotion"; potion.itemName = "검사 물약";
            weapon.itemID = "TestWeapon"; weapon.itemName = "검사 무기"; weapon.durability = 9;
            var reward = new Reward { items = new List<ItemReward> { new ItemReward { item = potion, quantity = 3 } } };
            var result = RewardService.Apply(reward, player, inventory, currency, 20);
            check(result.success && currency.GetAmount("Gold") == 80, "상점 비용 처리");
            check(inventory.inventoryItems.Count == 1 && ((ConsumableItem)inventory.inventoryItems[0]).quantity == 3, "복수 수량 스택 지급");
            check(!RewardService.Apply(reward, player, inventory, currency, 100).success && currency.GetAmount("Gold") == 80, "잔액 부족 시 무변경");
            for (int i = 1; i < inventory.Capacity; i++) inventory.inventoryItems.Add(weapon);
            var equipmentReward = new Reward { items = new List<ItemReward> { new ItemReward { item = weapon } } };
            check(!RewardService.Apply(equipmentReward, player, inventory, currency, 10).success && currency.GetAmount("Gold") == 80, "가방 부족 시 미결제");
            check(RewardService.Apply(reward, player, inventory, currency).success && ((ConsumableItem)inventory.inventoryItems[0]).quantity == 6, "가득 찬 가방의 기존 스택 추가");
            inventory.inventoryItems.RemoveAt(inventory.inventoryItems.Count - 1);
            check(RewardService.Apply(equipmentReward, player, inventory, currency).success && inventory.inventoryItems[inventory.inventoryItems.Count - 1] != weapon, "장비 정의 복제");
            player.TakeDamage(2);
            result = RewardService.Apply(new Reward { hpHeal = 50 }, player, inventory, currency);
            check(result.success && result.healed == 2 && player.CurrentHP == player.MaxHP, "실제 회복량");
            result = RewardService.Apply(new Reward { statIncrease = new BaseStats(1, 2, 3) }, player, inventory, currency);
            check(result.success && player.baseStats.dex == 2, "스탯 보상");
            result = RewardService.Apply(default, player, inventory, currency, 0, 999);
            check(result.success && currency.GetAmount("Gold") == 0 && result.message.Contains("80"), "강제 골드 손실 상한");
            definition.exitNode = exit;
            definition.options.Add(new EventOption { id = "buy", text = "구매", reward = reward, goldCost = 10 });
            check(EventDefinitionValidator.Validate(definition) == null, "정의 검사");
            definition.options.Add(new EventOption { id = "buy", text = "중복" });
            check(EventDefinitionValidator.Validate(definition) != null, "중복 선택 ID 거부");
            var oldSave = new SaveGameData { player = PlayerSaveData.FromPlayerData(player.GetCurrentData()) };
            oldSave.version = 3;
            oldSave.inventoryItems.Add(new InventoryItemSaveData { itemId = "TestPotion", quantity = 7 });
            check(SaveGameData.TryDeserialize(JsonUtility.ToJson(oldSave), out var migrated, out _) && migrated.inventoryItems[0].quantity == 7 && migrated.version == 4, "v3 수량 보존 마이그레이션");
            migrated.eventProgress.claimed.Add("Event01/buy");
            migrated.eventProgress.acceptedQuests.Add("Q01");
            check(SaveGameData.TryDeserialize(JsonUtility.ToJson(migrated), out var restored, out _) && restored.eventProgress.claimed.Count == 1 && restored.eventProgress.acceptedQuests[0] == "Q01", "수령 및 퀘스트 상태 저장 왕복");
            var row = JsonUtility.FromJson<NodeDataRaw>("{\"NodeID\":\"Event01\",\"EventDefinitionID\":\"Shop01\"}");
            check(row.EventDefinitionID == "Shop01", "새 시트 열 역직렬화");
            return $"Event System {passed}개 통과 (외부 시트/실제 세이브 변경 없음)";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(potion);
            UnityEngine.Object.DestroyImmediate(weapon);
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(exit);
        }
    }
}
