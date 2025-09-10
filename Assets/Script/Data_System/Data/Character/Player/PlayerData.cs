using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public string id;
    public string displayName;

    public BaseStats baseStats; // str, dex, con
    public int attackBonus;
    public int hpBonus;
    [Range(0f, 1f)] public float dodgeBonus;
    public int rangeBonus;

    // 현재 상태
    public int currentHP;
    public int exp;
    public int lv;

    public PlayerData()
    {
        id = "Player";
        displayName = "플레이어";
        baseStats = new BaseStats(0, 30, 0);
        attackBonus = 0;
        hpBonus = 0;
        dodgeBonus = 0f;
        rangeBonus = 0;

        exp = 0;
        lv = 1;
    }
}
