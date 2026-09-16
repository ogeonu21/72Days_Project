using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InventoryManager : SingleTon<InventoryManager>
{
    public List<BaseItem> inventoryItems = new List<BaseItem>();
    private int maxInventorySlot = 20;
    public bool isInventoryPull;

    protected override void Awake()
    {
        base.Awake();
    }

    public void MakeNew(ItemData itemData)
    {
        var restoredItems = itemData?.CreateRuntimeItems() ?? new List<BaseItem>();
        ReleaseRuntimeItems();
        inventoryItems = restoredItems;
        isInventoryPull = inventoryItems.Count >= maxInventorySlot;
    }

    public void AddToInventory(BaseItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.itemID)) return;
        // 로드 후에는 원본과 다른 인스턴스이므로 참조가 아니라 ID로 스택을 찾는다.
        if (item is ConsumableItem)
        {
            foreach (var existing in inventoryItems)
            {
                if (existing is ConsumableItem stack && stack.itemID == item.itemID)
                {
                    if (stack.quantity == int.MaxValue) return;
                    stack.quantity++;
                    return;
                }
            }
        }

        //인벤토리가 꽉차있다면
        if(inventoryItems.Count >= maxInventorySlot){
            isInventoryPull = true;
            Debug.Log("<color=green>[Inventory] </color>인벤토리가 꽉 차 더이상 아이템을 획득할 수 없습니다.");
            return;
        }

        inventoryItems.Add(item is ConsumableItem source ? source.CreateRuntimeCopy(1) : item);

        isInventoryPull = inventoryItems.Count >= maxInventorySlot;
        //인벤토리UI 업데이트
    }

    public void RemoveFromInventory(BaseItem item)
    {
        if (inventoryItems.Contains(item))
        {
            inventoryItems.Remove(item);
            ConsumableItem.ReleaseRuntimeCopy(item);
            Debug.Log($"<color=green>[Inventory] </color>아이템이 인벤토리에서 제거되었습니다. 현재 인벤토리에 들어있는 아이템은 {string.Join(", ", inventoryItems.ConvertAll(i => i.itemName))}입니다.");
        }
        else
        {
            Debug.Log("<color=green>[Inventory] </color>인벤토리에 존재하지 않는 아이템입니다.");
        }

        isInventoryPull = inventoryItems.Count >= maxInventorySlot;
        //인벤토리UI 업데이트
    }

    public void UpdateItemUI(BaseItem item)
    {
        
    }

    private void ReleaseRuntimeItems()
    {
        if (inventoryItems == null) return;
        foreach (var item in inventoryItems) ConsumableItem.ReleaseRuntimeCopy(item);
    }

    private void OnDestroy()
    {
        ReleaseRuntimeItems();
    }

}
