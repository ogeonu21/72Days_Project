using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public string id;
    public string displayName;

    public BaseStats baseStats; // str, dex, con
    public TuningStats tuningStats;
    public EquipmentData equipmentData;

    // 현재 상태
    public int currentHP;
    public int exp;
    public int lv;

    public PlayerData()
    {
        id = "Player";
        displayName = "플레이어";
        baseStats = new BaseStats(0, 0, 0);
        tuningStats = new TuningStats(0, 0, 0f, 0);
        equipmentData = new EquipmentData();

        exp = 0;
        lv = 1;
    }
}
