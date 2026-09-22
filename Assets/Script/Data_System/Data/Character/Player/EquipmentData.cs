using UnityEngine;
using System;

[Serializable]
public class EquipmentData
{
    public WeaponItem weaponItem;
    public const int ArmorSlotCount = 4;
    public ArmorItem[] armorItem = new ArmorItem[ArmorSlotCount];
    public AccessoryItem accessoryItem;
    public static bool IsValidArmorType(ArmorType type)
    {
        return (int)type >= 0 && (int)type < ArmorSlotCount;
    }

    public void EnsureArmorSlots()
    {
        if (armorItem == null) armorItem = new ArmorItem[ArmorSlotCount];
        else if (armorItem.Length != ArmorSlotCount) Array.Resize(ref armorItem, ArmorSlotCount);
    }

    // 컨테이너와 배열은 복사하고, 변경하지 않는 장비 정의만 공유한다.
    public EquipmentData Copy()
    {
        var copy = new EquipmentData { weaponItem = weaponItem, accessoryItem = accessoryItem };
        if (armorItem != null)
            Array.Copy(armorItem, copy.armorItem, Math.Min(armorItem.Length, ArmorSlotCount));
        return copy;
    }
    //장비 내구도도 복사하는가?
}
