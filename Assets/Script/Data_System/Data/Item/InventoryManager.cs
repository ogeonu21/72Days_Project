using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : SingleTon<InventoryManager>
{
    public List<BaseItem> inventoryItems = new List<BaseItem>();

    protected override void Awake()
    {
        base.Awake();
    }

    public void MakeNew(ItemData itemData)
    {
        this.inventoryItems = itemData.inventoryItems;
    }

    public void UseItem(BaseItem item)
    {
        if (inventoryItems.Contains(item))
        {
            item.Use(CharacterManager.Instance.currentPlayer);
            if (item.isConsumable)
            {
                inventoryItems.Remove(item);
            }
        }
    }
    public void AddToInventory(BaseItem item)
    {
        if (inventoryItems.Contains(item))
        {
            Debug.Log("이미 인벤토리에 존재하는 아이템입니다.");
            return;
        }
        inventoryItems.Add(item);
        Debug.Log($"<color=green>[Inventory]</color>현재 인벤토리에 들어있는 아이템은 {string.Join(", ", inventoryItems)}입니다.");
    }

}
