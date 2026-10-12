using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>원본 에셋/실제 savedata.json을 변경하지 않는 저장 및 장비 회귀 검사.</summary>
public static class ItemSaveValidation
{
    [MenuItem("Tools/Validation/Item Save and Armor Slots")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        int passed = 0;
        Action<bool, string> check = (ok, label) =>
        {
            if (!ok) throw new InvalidOperationException("검증 실패: " + label);
            passed++;
        };
        var definitions = new List<UnityEngine.Object>();
        var playerObject = new GameObject("ItemSaveValidation_Player") { hideFlags = HideFlags.HideAndDontSave };
        var inventoryObject = new GameObject("ItemSaveValidation_Inventory") { hideFlags = HideFlags.HideAndDontSave };
        var saveObject = new GameObject("ItemSaveValidation_Save") { hideFlags = HideFlags.HideAndDontSave };
        // Awake/OnEnable의 싱글톤 등록과 저장 이벤트 구독을 피한다.
        inventoryObject.SetActive(false);
        saveObject.SetActive(false);
        string temporaryDirectory = Path.Combine(Path.GetTempPath(), "Days-ItemSaveValidation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        var inventoryInstanceField = typeof(SingleTon<InventoryManager>).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        var originalInventoryInstance = inventoryInstanceField.GetValue(null);
        try
        {
            var player = playerObject.AddComponent<Player>();
            var inventory = inventoryObject.AddComponent<InventoryManager>();
            inventoryInstanceField.SetValue(null, inventory);
            var save = saveObject.AddComponent<SaveManager>();
            var lookup = new Dictionary<string, BaseItem>();
            Func<string, BaseItem> loader = id => lookup.TryGetValue(id, out var item) ? item : null;
            var armor = new ArmorItem[EquipmentData.ArmorSlotCount];
            for (int i = 0; i < armor.Length; i++)
            {
                armor[i] = ScriptableObject.CreateInstance<ArmorItem>();
                definitions.Add(armor[i]);
                armor[i].itemID = "TestArmor" + i;
                armor[i].itemCategory = ItemCategory.Armor;
                armor[i].armorType = (ArmorType)i;
                armor[i].durability = 100;
                armor[i].hpBonus = i + 1;
                armor[i].dodgeBonus = .01f;
                lookup.Add(armor[i].itemID, armor[i]);
            }
            var weapon = ScriptableObject.CreateInstance<WeaponItem>();
            definitions.Add(weapon);
            weapon.itemID = "TestWeapon";
            weapon.itemCategory = ItemCategory.Weapon;
            weapon.durability = 100;
            weapon.attackBonus = 9;
            weapon.range = 3;
            lookup.Add(weapon.itemID, weapon);
            var accessory = ScriptableObject.CreateInstance<AccessoryItem>();
            definitions.Add(accessory);
            accessory.itemID = "TestAccessory";
            accessory.itemCategory = ItemCategory.Accessory;
            accessory.durability = 100;
            accessory.dodgeBonus = .02f;
            lookup.Add(accessory.itemID, accessory);
            var potion = ScriptableObject.CreateInstance<PotionItem>();
            definitions.Add(potion);
            potion.itemID = "TestPotion";
            potion.itemCategory = ItemCategory.Potion;
            potion.isConsumable = true;
            potion.health = 2;
            potion.quantity = 99;
            lookup.Add(potion.itemID, potion);

            player.InitializeFromData(new PlayerData());
            player.EquipItem(weapon);
            player.EquipItem(accessory);
            foreach (var part in armor) player.EquipItem(part);
            check(player.MaxHP == 40 && Mathf.Approximately(player.tuningStats.dodgeBonus, .06f), "방어구 4부위 합산");
            check(player.tuningStats.rangeBonus == 3 && player.tuningStats.attackBonus == 9, "무기 사거리/공격력");
            player.EquipItem(armor[0]);
            check(player.MaxHP == 40 && player.equipmentData.armorItem[1] == armor[1], "같은 부위 재장착");
            var replacement = UnityEngine.Object.Instantiate(armor[0]);
            definitions.Add(replacement);
            replacement.hpBonus = 6;
            player.EquipItem(replacement);
            check(player.MaxHP == 45 && player.equipmentData.armorItem[2] == armor[2], "한 부위 교체, 나머지 유지");
            player.ReleaseItem(armor[0]);
            check(player.equipmentData.armorItem[0] == replacement, "미장착 장비 해제 무시");
            player.EquipItem(armor[0]);
            player.ReleaseItem(armor[1]);
            check(player.equipmentData.armorItem.Length == 4 && player.equipmentData.armorItem[1] == null && player.MaxHP == 38, "한 부위 해제");
            player.ReleaseItem(armor[1]);
            check(player.MaxHP == 38, "반복 해제");
            player.EquipItem(armor[1]);
            var playerData = player.GetCurrentData();
            playerData.equipmentData.armorItem[0] = null;
            check(player.equipmentData.armorItem[0] == armor[0], "스냅샷 배열 격리");
            var input = new EquipmentData { armorItem = null };
            player.RestoreEquipment(input);
            check(player.equipmentData.armorItem.Length == 4, "null 배열 복원");
            input.armorItem = new[] { armor[0] };
            player.RestoreEquipment(input);
            input.armorItem[0] = null;
            check(player.equipmentData.armorItem.Length == 4 && player.equipmentData.armorItem[0] == armor[0], "짧은 배열 및 복사 격리");
            player.EquipItem(weapon);
            player.EquipItem(accessory);
            foreach (var part in armor) player.EquipItem(part);
            player.TakeDamage(8);
            int savedHP = player.CurrentHP;

            inventory.MakeNew(new ItemData());
            for (int i = 0; i < 7; i++) inventory.AddToInventory(potion);
            inventory.AddToInventory(weapon);
            var stack = (PotionItem)inventory.inventoryItems[0];
            check(stack != potion && stack.quantity == 7 && potion.quantity == 99, "첫 획득 1개 및 원본 수량 격리");
            var snapshot = new SaveGameData { currentNodeId = "Main_01", player = PlayerSaveData.FromPlayerData(player.GetCurrentData()) };
            SaveManager.CaptureItems(snapshot, inventory.inventoryItems, player.equipmentData);
            check(snapshot.inventoryItems[0].quantity == 7 && snapshot.inventoryItems[1].quantity == 1, "아이템별 수량 저장");
            string json = JsonUtility.ToJson(snapshot);
            check(!json.Contains("instanceID") && json.Contains("armorItemIds"), "Unity 참조 없는 ID 저장");
            typeof(SaveManager).GetField("savePath", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(save, Path.Combine(temporaryDirectory, "savedata.json"));
            var write = typeof(SaveManager).GetMethod("WriteAtomically", BindingFlags.Instance | BindingFlags.NonPublic);
            write.Invoke(save, new object[] { json });
            check(save.TryLoadGame(out var loaded, out var error), "임시 파일 신규 저장/읽기: " + error);
            write.Invoke(save, new object[] { json });
            check(save.TryLoadGame(out loaded, out error), "임시 파일 원자적 교체/읽기: " + error);
            check(SaveManager.TryRestoreItems(loaded, out var restoredItems, out var restoredEquipment, out error, loader), "아이템 복원: " + error);
            for (int i = 0; i < armor.Length; i++) check(restoredEquipment.armorItem[i] == armor[i], "슬롯 " + i + " 왕복");
            check(restoredEquipment.weaponItem == weapon && restoredEquipment.accessoryItem == accessory, "무기/장신구 왕복");
            var loadedPlayer = loaded.player.ToPlayerData();
            loadedPlayer.equipmentData = restoredEquipment;
            player.LoadFromData(loadedPlayer);
            check(player.CurrentHP == savedHP && player.MaxHP == 40, "HP 및 장비 보정 왕복");
            inventory.MakeNew(restoredItems);
            check(stack == null, "이전 런타임 소모품 정리");
            stack = (PotionItem)inventory.inventoryItems[0];
            check(stack.quantity == 7 && potion.quantity == 99, "소모품 수량 복원 및 에셋 불변");
            inventory.MakeNew(restoredItems);
            stack = (PotionItem)inventory.inventoryItems[0];
            check(stack.quantity == 7, "반복 로드 시 수량 중복 없음");
            inventory.AddToInventory(potion);
            check(stack.quantity == 8 && inventory.inventoryItems.Count == 2, "로드 후 같은 ID 스택 합산");
            stack.Use(player);
            check(stack.quantity == 7 && player.CurrentHP == savedHP + 2, "로드 후 소모품 사용");
            SaveManager.CaptureItems(snapshot, inventory.inventoryItems, player.equipmentData);
            check(snapshot.inventoryItems[0].quantity == 7, "사용 후 재저장");
            for (int i = inventory.inventoryItems.Count; i < 20; i++) inventory.AddToInventory(weapon);
            inventory.AddToInventory(weapon);
            check(inventory.inventoryItems.Count == 20 && inventory.isInventoryPull, "슬롯 상한");
            inventory.AddToInventory(potion);
            check(stack.quantity == 8, "가득 찬 인벤토리 기존 스택 추가");
            inventory.RemoveFromInventory(stack);
            inventory.AddToInventory(potion);
            check(((ConsumableItem)inventory.inventoryItems[19]).quantity == 1 && potion.quantity == 99, "제거 후 재획득 1개");
            var lastPotion = (PotionItem)inventory.inventoryItems[19];
            lastPotion.Use(player);
            check(inventory.inventoryItems.Count == 19 && lastPotion == null && !inventory.isInventoryPull, "마지막 소모품 사용 후 제거");
            inventory.MakeNew(new ItemData());
            check(inventory.inventoryItems.Count == 0 && !inventory.isInventoryPull, "새 게임 초기화");
            SaveManager.CaptureItems(snapshot, inventory.inventoryItems, new EquipmentData());
            check(SaveGameData.TryDeserialize(JsonUtility.ToJson(snapshot), out var emptySave, out _) &&
                SaveManager.TryRestoreItems(emptySave, out var emptyItems, out var emptyEquipment, out _, loader) &&
                emptyItems.inventoryItems.Count == 0 && emptyEquipment.armorItem.Length == 4 && emptyEquipment.armorItem[0] == null,
                "빈 인벤토리/빈 장비 왕복");

            string legacy = "{\"version\":2,\"player\":{\"id\":\"Player\"},\"armorItemId\":\"TestArmor3\",\"inventoryItemIds\":[\"TestPotion\",\"TestPotion\",\"TestWeapon\"]}";
            check(SaveGameData.TryDeserialize(legacy, out loaded, out error), "v2 단일 방어구 이전 파싱");
            check(SaveManager.TryRestoreItems(loaded, out restoredItems, out restoredEquipment, out error, loader) && restoredEquipment.armorItem[3] == armor[3], "구 방어구를 실제 부위로 이전");
            check(SaveGameData.TryDeserialize(legacy.Replace("\"version\":2", "\"version\":1"), out var v1, out _) &&
                SaveManager.TryRestoreItems(v1, out _, out var v1Equipment, out _, loader) && v1Equipment.armorItem[3] == armor[3], "v1 방어구 이전");
            inventory.MakeNew(restoredItems);
            check(((ConsumableItem)inventory.inventoryItems[0]).quantity == 2, "구 ID 횟수로 최소 수량 이전");
            SaveManager.CaptureItems(loaded, inventory.inventoryItems, restoredEquipment);
            check(SaveGameData.TryDeserialize(JsonUtility.ToJson(loaded), out loaded, out error) && loaded.armorItemIds[3] == armor[3].itemID, "구 파일 -> v3 재저장");
            loaded.armorItemIds[0] = "Missing";
            check(!SaveManager.TryRestoreItems(loaded, out _, out _, out error, loader), "없는 아이템은 복원 실패");
            loaded.armorItemIds[0] = armor[1].itemID;
            check(!SaveManager.TryRestoreItems(loaded, out _, out _, out error, loader), "부위 불일치 거부");
            loaded.armorItemIds[0] = weapon.itemID;
            check(!SaveManager.TryRestoreItems(loaded, out _, out _, out error, loader), "잘못된 장비 타입 거부");
            loaded.armorItemIds[0] = null;
            loaded.inventoryItems[0].quantity = -1;
            check(!SaveGameData.TryDeserialize(JsonUtility.ToJson(loaded), out _, out _), "음수 수량 거부");
            loaded.inventoryItems[0].quantity = 0;
            check(!SaveGameData.TryDeserialize(JsonUtility.ToJson(loaded), out _, out _), "0개 저장 항목 거부");
            check(!SaveGameData.TryDeserialize("{broken", out _, out _), "손상 JSON 거부");
            loaded.inventoryItems[0].quantity = 1;
            loaded.inventoryItems[0].itemId = "MissingPotion";
            check(!SaveManager.TryRestoreItems(loaded, out var failedItems, out var failedEquipment, out error, loader) &&
                failedItems == null && failedEquipment == null, "누락 인벤토리 아이템: 부분 복원 없이 실패");
            loaded.armorItemIds = new string[2];
            check(!SaveGameData.TryDeserialize(JsonUtility.ToJson(loaded), out _, out _), "잘못된 슬롯 개수 거부");

            var resourceSnapshot = new SaveGameData();
            var resourceEquipment = new EquipmentData();
            foreach (var definition in Resources.LoadAll<ArmorItem>("Items"))
            {
                check(EquipmentData.IsValidArmorType(definition.armorType), "실제 방어구 부위: " + definition.itemID);
                resourceEquipment.armorItem[(int)definition.armorType] = definition;
            }
            SaveManager.CaptureItems(resourceSnapshot, Resources.LoadAll<BaseItem>("Items"), resourceEquipment);
            check(SaveManager.TryRestoreItems(resourceSnapshot, out _, out _, out error), "실제 Resources 아이템 로드: " + error);
            return $"Item/Save 검사 {passed}개 통과 (임시 파일 왕복 포함, 실제 저장 파일 변경 없음)";
        }
        finally
        {
            inventoryInstanceField.SetValue(null, originalInventoryInstance);
            UnityEngine.Object.DestroyImmediate(inventoryObject);
            UnityEngine.Object.DestroyImmediate(saveObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
            foreach (var item in definitions) UnityEngine.Object.DestroyImmediate(item);
            // 이 검사에서 생성한 고유 임시 폴더만 정리한다.
            foreach (string file in Directory.GetFiles(temporaryDirectory)) File.Delete(file);
            Directory.Delete(temporaryDirectory);
        }
    }
}
