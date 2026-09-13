using UnityEngine;

[System.Serializable]
public class ItemDataRaw
{
    #region [공통]
    public string ItemID;
    public string ItemName;
    public string ItemDesc;
    public string ItemIcon;
    public bool Consumable;
    public int ItemValue;
    public string ItemCategory;
    #endregion

    #region [무기]
    public int Durability; //내구도
    public int AttackBonus;
    public int Range;
    #endregion

    #region [방어구&악세사리]
    public string ArmorType;
    public int HpBonus;
    public float DodgeBonus;
    public string QuestID;
    #endregion

    #region [포션]
    public int Health;
    #endregion
}
