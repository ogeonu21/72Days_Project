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

    public void AddToInventory(BaseItem item)
    {
        if (inventoryItems.Contains(item))
        {
            Debug.Log("<color=green>[Inventory] </color>이미 인벤토리에 존재하는 아이템입니다.");
            return;
        }
        inventoryItems.Add(item);
        Debug.Log($"<color=green>[Inventory] </color>현재 인벤토리에 들어있는 아이템은 {string.Join(", ", inventoryItems.ConvertAll(i => i.itemName))}입니다.");
    }

    public void RemoveFromInventory(BaseItem item)
    {
        if (inventoryItems.Contains(item))
        {
            inventoryItems.Remove(item);
            Debug.Log($"<color=green>[Inventory] </color>아이템이 인벤토리에서 제거되었습니다. 현재 인벤토리에 들어있는 아이템은 {string.Join(", ", inventoryItems.ConvertAll(i => i.itemName))}입니다.");
        }
        else
        {
            Debug.Log("<color=green>[Inventory] </color>인벤토리에 존재하지 않는 아이템입니다.");
        }
    }

}
