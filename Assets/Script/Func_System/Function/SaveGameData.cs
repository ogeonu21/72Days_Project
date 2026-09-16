using System;
using System.Collections.Generic;

[Serializable]
public class SaveGameData
{
    public const float CurrentVersion = 3.0f;

    public float version = CurrentVersion;
    public string currentNodeId;
    public PlayerSaveData player = new PlayerSaveData();
    public List<CurrencySaveData> currencies = new List<CurrencySaveData>();
    public List<InventoryItemSaveData> inventoryItems = new List<InventoryItemSaveData>();
    public string weaponItemId;
    public string[] armorItemIds = new string[EquipmentData.ArmorSlotCount];
    public string accessoryItemId;
    [NonSerialized] public string legacyArmorItemId;

    public static bool TryDeserialize(string json, out SaveGameData data, out string error)
    {
        data = null;
        error = null;
        try
        {
            var header = UnityEngine.JsonUtility.FromJson<LegacyHeader>(json);
            if (header == null || (header.version != 1.0f && header.version != 2.0f && header.version != CurrentVersion))
            {
                error = "지원하지 않거나 버전이 없는 저장 파일입니다.";
                return false;
            }
            data = UnityEngine.JsonUtility.FromJson<SaveGameData>(json);
            // JsonUtility는 JSON null을 기본 객체로 복원할 수 있어 필수 ID도 검사한다.
            if (data == null || data.player == null || string.IsNullOrWhiteSpace(data.player.id))
            {
                data = null;
                error = "플레이어 저장 데이터가 없습니다.";
                return false;
            }
            if (header.version < CurrentVersion)
            {
                // 구 파일에는 수량이 기록되지 않았다. ID 등장 횟수만큼(보통 1개) 복원한다.
                data.inventoryItems = new List<InventoryItemSaveData>();
                if (header.inventoryItemIds != null)
                {
                    foreach (string id in header.inventoryItemIds)
                    {
                        if (string.IsNullOrWhiteSpace(id)) throw new FormatException("구 인벤토리에 빈 ID가 있습니다.");
                        data.inventoryItems.Add(new InventoryItemSaveData { itemId = id, quantity = 1 });
                    }
                }
                data.legacyArmorItemId = header.armorItemId;
                data.armorItemIds = new string[EquipmentData.ArmorSlotCount];
            }
            if (data.armorItemIds == null) data.armorItemIds = new string[EquipmentData.ArmorSlotCount];
            if (data.armorItemIds.Length != EquipmentData.ArmorSlotCount)
                throw new FormatException("방어구 슬롯은 4개여야 합니다.");
            if (data.inventoryItems == null) data.inventoryItems = new List<InventoryItemSaveData>();
            foreach (var item in data.inventoryItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemId) || item.quantity <= 0)
                    throw new FormatException("인벤토리 ID 또는 수량이 올바르지 않습니다.");
            }
            if (header.version == 1.0f)
            {
                if (header.tendency != 0 && header.goodAndEvil != 0 && header.tendency != header.goodAndEvil)
                {
                    data = null;
                    error = "구 저장 파일의 두 성향 값이 충돌합니다.";
                    return false;
                }
                data.player.tendency = header.tendency != 0 ? header.tendency : header.goodAndEvil;
            }
            data.version = CurrentVersion;
            return true;
        }
        catch (Exception exception)
        {
            data = null;
            error = "저장 데이터 해석 실패: " + exception.Message;
            return false;
        }
    }

    [Serializable]
    private class LegacyHeader
    {
        public float version;
        public int goodAndEvil;
        public int tendency;
        public string armorItemId;
        public List<string> inventoryItemIds;
    }
}

[Serializable]
public class InventoryItemSaveData
{
    public string itemId;
    public int quantity;
}

[Serializable]
public class PlayerSaveData
{
    public string id;
    public string displayName;
    public BaseStats baseStats;
    public TuningStats tuningStats;
    public int currentHP;
    public int exp;
    public int level;
    public int tendency;

    public static PlayerSaveData FromPlayerData(PlayerData source)
    {
        return new PlayerSaveData
        {
            id = source.id,
            displayName = source.displayName,
            baseStats = source.baseStats,
            tuningStats = source.tuningStats,
            currentHP = source.currentHP,
            exp = source.exp,
            level = source.lv,
            tendency = source.tendency
        };
    }

    public PlayerData ToPlayerData()
    {
        return new PlayerData
        {
            id = id,
            displayName = displayName,
            baseStats = baseStats,
            tuningStats = tuningStats,
            currentHP = currentHP,
            exp = exp,
            lv = level,
            tendency = tendency,
            equipmentData = new EquipmentData()
        };
    }
}

[Serializable]
public class CurrencySaveData
{
    public string name;
    public int amount;

    public CurrencySaveData() { }

    public CurrencySaveData(string name, int amount)
    {
        this.name = name;
        this.amount = amount;
    }
}
