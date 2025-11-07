using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "WeaponItem", menuName = "Items/Equipment/Weapon")]
public class WeaponItem : EquipmentItem
{
    [Header("Weapon Stats")]
    public int bonusAttackPower;
    public int attackDistance;

}