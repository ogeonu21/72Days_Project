using UnityEngine;

public enum ArmorType
{
    Helmet,
    Chestplate,
    Leggings,
    Boots
}

[System.Serializable]
[CreateAssetMenu(fileName = "ArmorItem", menuName = "Items/Equipment/Armor")]
public class ArmorItem : EquipmentItem
{
    public ArmorType armorType;   
    public int hpBonus;
    public float dodgeBonus;


}