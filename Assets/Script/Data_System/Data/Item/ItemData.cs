using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemData
{
    public List<BaseItem> inventoryItems;
    // 로드 -> InventoryManager 초기화 사이에만 사용하는 수량. 원본 에셋은 변경하지 않는다.
    public Dictionary<string, int> consumableQuantities = new Dictionary<string, int>();

    public ItemData()
    {
        inventoryItems = new List<BaseItem>();
    }

    public List<BaseItem> CreateRuntimeItems()
    {
        var result = new List<BaseItem>();
        if (inventoryItems == null) return result;
        foreach (var item in inventoryItems)
        {
            if (item == null) continue;
            if (item is ConsumableItem consumable)
            {
                int quantity = consumableQuantities != null && consumableQuantities.TryGetValue(item.itemID, out int saved)
                    ? saved : Mathf.Max(1, consumable.quantity);
                if (quantity > 0) result.Add(consumable.CreateRuntimeCopy(quantity));
            }
            else result.Add(item);
        }
        return result;
    }
}
