using System;
using System.Collections.Generic;

[Serializable]
public class SaveGameData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public string currentNodeId;
    public int goodAndEvil;
    public PlayerSaveData player = new PlayerSaveData();
    public List<CurrencySaveData> currencies = new List<CurrencySaveData>();
    public List<string> inventoryItemIds = new List<string>();
    public string weaponItemId;
    public string armorItemId;
    public string accessoryItemId;
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
            level = source.lv
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
