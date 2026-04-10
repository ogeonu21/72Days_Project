using UnityEngine;

[System.Serializable]
public class ItemDataRaw
{
    public string ItemID;
    public string ItemName;
    public string ItemDesc;
    public Sprite ItemIcon;
    public bool Consumable;
    public int ItemValue;
    public ItemCategory ItemCategory;
    public int Durability; //³»±¸µµ
    public int AttackBonus;
    public int Range;
    public int HpBonus;
    public float DodgeBonus;
    public string QuestID;
    public int Health;
}
