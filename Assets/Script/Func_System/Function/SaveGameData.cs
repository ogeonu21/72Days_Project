using System;
using System.Collections.Generic;

[Serializable]
public class SaveGameData
{
    public const float CurrentVersion = 2.0f;

    public float version = CurrentVersion;
    public string currentNodeId;
    public PlayerSaveData player = new PlayerSaveData();
    public List<CurrencySaveData> currencies = new List<CurrencySaveData>();
    public List<string> inventoryItemIds = new List<string>();
    public string weaponItemId;
    public string armorItemId;
    public string accessoryItemId;

    public static bool TryDeserialize(string json, out SaveGameData data, out string error)
    {
        data = null;
        error = null;
        try
        {
            var header = UnityEngine.JsonUtility.FromJson<LegacyHeader>(json);
            if (header == null || (header.version != 1.0f && header.version != CurrentVersion))
            {
                error = "지원하지 않거나 버전이 없는 저장 파일입니다.";
                return false;
            }
            data = UnityEngine.JsonUtility.FromJson<SaveGameData>(json);
            // JsonUtility는 JSON null을 기본 객체로 복원할 수 있어 필수 ID도 검사한다.
            if (data == null || data.player == null || string.IsNullOrWhiteSpace(data.player.id))
            {
                error = "플레이어 저장 데이터가 없습니다.";
                return false;
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
    }
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
