using UnityEngine;

[System.Serializable]
public class EquipmentItem : BaseItem
{
    public int durability; //내구도

    
    public override void Use(Player player)
    {
        if (--durability <= 0)
        {
            //장비가 파괴되어야겠지?
            player.ReleaseItem(this);
            InventoryManager.Instance.RemoveFromInventory(this);

        }
        return;
    }

    public void Release(Player player)
    {
        if (player == null || player.equipmentData == null) return;
        player.equipmentData.EnsureArmorSlots();
        switch (itemCategory)
        {
            case ItemCategory.Weapon:
                if (player.equipmentData.weaponItem == this) player.equipmentData.weaponItem = null;
                break;
            case ItemCategory.Armor:
                if (this is ArmorItem armor && EquipmentData.IsValidArmorType(armor.armorType) &&
                    player.equipmentData.armorItem[(int)armor.armorType] == armor)
                    player.equipmentData.armorItem[(int)armor.armorType] = null;
                //방어구 해제 로직
                break;
            case ItemCategory.Accessory:
                if (player.equipmentData.accessoryItem == this) player.equipmentData.accessoryItem = null;
                //악세서리 해제 로직
                break;
            default:
                Debug.LogWarning("알 수 없는 장비 유형입니다.");
                break;
        }
    }
}

