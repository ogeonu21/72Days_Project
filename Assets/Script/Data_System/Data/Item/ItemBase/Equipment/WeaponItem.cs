using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "WeaponItem", menuName = "Items/Equipment/Weapon")]
public class WeaponItem : EquipmentItem
{
    [Header("Weapon Stats")]
    public int attackBonus;
    public int range;

}