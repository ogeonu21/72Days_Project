using UnityEngine;

[System.Serializable]
public class EquipmentItem : BaseItem
{
    public int durability; //내구도

    // public int weaponDamageAmount;
    // public int weaponAttackDistance;
    public override void Equip(Player player)
    {
        //아이템 장착 함수.
        //튜닝 수치 업데이트.
        //player에게 장착 아이템 정보 제공 및 수치 업데이트.
        if (durability <= 0)
        {
            Debug.Log("내구도가 0이하입니다. 장착할 수 없습니다.");
            Release(player);
            return;
        }
    }
    public override void Use(Player player)
    {
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
                //무기 해제 로직
                //player.equipmentData.weaponId = -1;
                //palyer.UpdateEquipmentStats();
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
            case ItemCategory.Potion:
                // 포션은 장비가 아니므로 해제 로직이 필요 없음
                break;
            default:
                Debug.LogWarning("알 수 없는 장비 유형입니다.");
                break;
        }

        //아이템 해제.
        //player에게 아이템 해제하도록 명령, 수치 업데이트.
    }
}

