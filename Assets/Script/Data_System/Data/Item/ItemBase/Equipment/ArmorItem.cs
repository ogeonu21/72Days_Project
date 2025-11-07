using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "ArmorItem", menuName = "Items/Equipment/Armor")]
public class ArmorItem : EquipmentItem
{
    public int bonusHp;
    public float bonusDodge;


}