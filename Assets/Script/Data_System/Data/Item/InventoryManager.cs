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

}
