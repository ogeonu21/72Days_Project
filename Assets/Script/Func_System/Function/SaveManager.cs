using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : SingleTon<SaveManager>
{
    private string savePath;

    protected override void Awake()
    {
        base.Awake();
        savePath = Path.Combine(Application.persistentDataPath, "savedata.json");
    }

    protected void OnEnable()
    {
        GameEvent.OnSaveGame += SaveGame;
    }

    protected void OnDisable()
    {
        GameEvent.OnSaveGame -= SaveGame;
    }

    public bool TryLoadGame(out SaveGameData data, out string error)
    {
        data = null;
        error = null;

        if (!File.Exists(savePath))
        {
            error = "저장 파일이 없습니다.";
            return false;
        }

        try
        {
            return SaveGameData.TryDeserialize(File.ReadAllText(savePath), out data, out error);
        }
        catch (Exception exception)
        {
            error = $"저장 파일을 읽지 못했습니다: {exception.Message}";
            return false;
        }
    }

    public bool TryRestoreGame(out PlayerData playerData, out ItemData itemData, out List<CurrencyData> currencies,
        out Node currentNode, out EquipmentData equipmentData, out string error)
    {
        playerData = null;
        itemData = null;
        currencies = null;
        currentNode = null;
        equipmentData = null;

        if (!TryLoadGame(out SaveGameData data, out error))
        {
            return false;
        }

        currentNode = LoadNode(data.currentNodeId);
        if (currentNode == null)
        {
            error = $"저장된 노드를 찾을 수 없습니다: {data.currentNodeId}";
            return false;
        }

        if (!TryRestoreItems(data, out itemData, out equipmentData, out error)) return false;
        playerData = data.player.ToPlayerData();
        currencies = LoadCurrencies(data.currencies);
        playerData.equipmentData = equipmentData;
        return true;
    }

    public void SaveGame()
    {
        Player player = CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null;
        Node node = NodeManager.Instance != null ? NodeManager.Instance.currentNode : null;
        if (player == null || node == null)
        {
            Debug.LogWarning("[SaveManager] 플레이어 또는 현재 노드가 준비되지 않아 저장하지 않았습니다.");
            return;
        }

        try
        {
            SaveGameData data = CreateSnapshot(player, node);
            WriteAtomically(JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveManager] 저장 데이터 생성 실패(기존 파일 유지): {exception.Message}");
        }
    }

    private SaveGameData CreateSnapshot(Player player, Node node)
    {
        SaveGameData data = new SaveGameData
        {
            currentNodeId = node.name,
            player = PlayerSaveData.FromPlayerData(player.GetCurrentData())
        };

        if (CurrencyManager.Instance != null && CurrencyManager.Instance.currencyList != null)
        {
            foreach (CurrencyData currency in CurrencyManager.Instance.currencyList)
            {
                if (currency != null)
                {
                    data.currencies.Add(new CurrencySaveData(currency.Name, currency.Amount));
                }
            }
        }

        CaptureItems(data, InventoryManager.Instance != null ? InventoryManager.Instance.inventoryItems : null,
            player.equipmentData);

        return data;
    }

    private void WriteAtomically(string json)
    {
        string temporaryPath = savePath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(savePath))
            {
                File.Replace(temporaryPath, savePath, null);
            }
            else
            {
                File.Move(temporaryPath, savePath);
            }

            Debug.Log($"[SaveManager] 저장 완료: {savePath}");
        }
        catch (Exception exception)
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            Debug.LogError($"[SaveManager] 저장 실패: {exception.Message}");
        }
    }

    private static string GetItemId(BaseItem item)
    {
        if (item != null && string.IsNullOrWhiteSpace(item.itemID))
            throw new InvalidOperationException("ID가 없는 아이템은 저장할 수 없습니다.");
        return item != null ? item.itemID : null;
    }

    private static Node LoadNode(string nodeId)
    {
        return string.IsNullOrWhiteSpace(nodeId) ? null : Resources.Load<Node>($"Nodes/{nodeId}");
    }

    private static BaseItem LoadItem(string itemId)
    {
        return string.IsNullOrWhiteSpace(itemId) ? null : Resources.Load<BaseItem>($"Items/{itemId}");
    }

    public static void CaptureItems(SaveGameData data, IEnumerable<BaseItem> items, EquipmentData equipment)
    {
        data.inventoryItems = new List<InventoryItemSaveData>();
        if (items != null)
        {
            foreach (BaseItem item in items)
            {
                if (item == null) throw new InvalidOperationException("인벤토리에 누락된 아이템이 있습니다.");
                int quantity = item is ConsumableItem consumable ? consumable.quantity : 1;
                if (quantity < 0) throw new InvalidOperationException("소모품 수량이 음수입니다.");
                if (quantity == 0) continue;
                data.inventoryItems.Add(new InventoryItemSaveData { itemId = GetItemId(item), quantity = quantity });
            }
        }
        data.weaponItemId = GetItemId(equipment?.weaponItem);
        data.accessoryItemId = GetItemId(equipment?.accessoryItem);
        data.armorItemIds = new string[EquipmentData.ArmorSlotCount];
        data.legacyArmorItemId = null;
        if (equipment?.armorItem == null) return;
        for (int i = 0; i < equipment.armorItem.Length; i++)
        {
            ArmorItem armor = equipment.armorItem[i];
            if (armor == null) continue;
            if (i >= EquipmentData.ArmorSlotCount || (int)armor.armorType != i)
                throw new InvalidOperationException("방어구 부위와 장착 슬롯이 일치하지 않습니다.");
            data.armorItemIds[i] = GetItemId(armor);
        }
    }

    // 정의 로드와 검증을 끝낸 후에만 결과를 전달한다. 실패한 아이템을 조용히 버리지 않는다.
    public static bool TryRestoreItems(SaveGameData data, out ItemData itemData,
        out EquipmentData equipmentData, out string error, Func<string, BaseItem> itemLoader = null)
    {
        itemData = null;
        equipmentData = null;
        error = null;
        try
        {
            if (data == null) throw new FormatException("저장 데이터가 없습니다.");
            Func<string, BaseItem> loader = itemLoader ?? LoadItem;
            var restoredItems = new ItemData();
            var restoredEquipment = new EquipmentData
            {
                weaponItem = LoadEquipment<WeaponItem>(data.weaponItemId, ItemCategory.Weapon, loader),
                accessoryItem = LoadEquipment<AccessoryItem>(data.accessoryItemId, ItemCategory.Accessory, loader)
            };
            if (data.armorItemIds == null || data.armorItemIds.Length != EquipmentData.ArmorSlotCount)
                throw new FormatException("방어구 슬롯은 4개여야 합니다.");
            for (int i = 0; i < EquipmentData.ArmorSlotCount; i++)
            {
                var armor = LoadEquipment<ArmorItem>(data.armorItemIds[i], ItemCategory.Armor, loader);
                if (armor != null && (int)armor.armorType != i)
                    throw new FormatException($"방어구 슬롯/부위 불일치: {armor.itemID}");
                restoredEquipment.armorItem[i] = armor;
            }
            if (!string.IsNullOrWhiteSpace(data.legacyArmorItemId))
            {
                var armor = LoadEquipment<ArmorItem>(data.legacyArmorItemId, ItemCategory.Armor, loader);
                if (!EquipmentData.IsValidArmorType(armor.armorType))
                    throw new FormatException("구 방어구의 부위가 올바르지 않습니다.");
                restoredEquipment.armorItem[(int)armor.armorType] = armor;
            }
            if (data.inventoryItems != null)
            {
                foreach (var entry in data.inventoryItems)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.itemId) || entry.quantity <= 0)
                        throw new FormatException("인벤토리 ID 또는 수량이 올바르지 않습니다.");
                    BaseItem item = LoadRequiredItem(entry.itemId, loader);
                    if (item is ConsumableItem)
                    {
                        if (restoredItems.consumableQuantities.TryGetValue(entry.itemId, out int previous))
                        {
                            restoredItems.consumableQuantities[entry.itemId] = checked(previous + entry.quantity);
                            continue;
                        }
                        restoredItems.consumableQuantities.Add(entry.itemId, entry.quantity);
                    }
                    else if (entry.quantity != 1)
                        throw new FormatException("장비 항목의 수량은 1이어야 합니다.");
                    restoredItems.inventoryItems.Add(item);
                }
            }
            itemData = restoredItems;
            equipmentData = restoredEquipment;
            return true;
        }
        catch (Exception exception)
        {
            error = "아이템 복원 실패: " + exception.Message;
            return false;
        }
    }

    private static BaseItem LoadRequiredItem(string id, Func<string, BaseItem> loader)
    {
        BaseItem item = loader(id);
        if (item == null || item.itemID != id) throw new FormatException($"아이템을 찾지 못했습니다: {id}");
        return item;
    }

    private static T LoadEquipment<T>(string id, ItemCategory category, Func<string, BaseItem> loader) where T : EquipmentItem
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var item = LoadRequiredItem(id, loader) as T;
        if (item == null || item.itemCategory != category || item.durability <= 0)
            throw new FormatException($"장비 타입 또는 내구도가 올바르지 않습니다: {id}");
        return item;
    }

    private static List<CurrencyData> LoadCurrencies(List<CurrencySaveData> savedCurrencies)
    {
        List<CurrencyData> currencies = new List<CurrencyData>();
        if (savedCurrencies == null)
        {
            return currencies;
        }

        foreach (CurrencySaveData savedCurrency in savedCurrencies)
        {
            if (savedCurrency != null && !string.IsNullOrWhiteSpace(savedCurrency.name))
            {
                currencies.Add(new CurrencyData(savedCurrency.name, savedCurrency.amount));
            }
        }

        return currencies;
    }
}
