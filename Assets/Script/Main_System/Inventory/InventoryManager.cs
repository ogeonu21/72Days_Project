using System.Collections;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class InventoryManager : SingleTon<InventoryManager>
{
    public List<BaseItem> inventoryItems = new List<BaseItem>();
    private int maxInventorySlot = 20;
    public bool isInventoryPull;
    public int Capacity => maxInventorySlot;
    public event Action InventoryChanged;

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
        InventoryChanged?.Invoke();
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
                    InventoryChanged?.Invoke();
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
        InventoryChanged?.Invoke();
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
        InventoryChanged?.Invoke();
    }

    public void UpdateItemUI(BaseItem item)
    {
        if (item != null && inventoryItems.Contains(item)) InventoryChanged?.Invoke();
    }

    public bool CanAdd(BaseItem item)
    {
        if (item == null) return false;
        if (item is ConsumableItem)
            foreach (var existing in inventoryItems)
                if (existing is ConsumableItem stack && stack.itemID == item.itemID)
                    return stack.quantity < int.MaxValue;
        return inventoryItems.Count < Capacity;
    }

    public bool TryEquipAt(int index, Player player, out string error)
    {
        error = null;
        if (!TryGetItem(index, out var item) || !(item is EquipmentItem equipment)) error = "장착할 장비를 선택하세요.";
        else if (player == null || player.IsDead) error = "현재 장비를 변경할 수 없습니다.";
        else if (equipment.durability <= 0) error = "내구도가 없는 장비입니다.";
        else
        {
            player.EquipItem(equipment);
            if (IsEquipped(player, equipment)) { InventoryChanged?.Invoke(); return true; }
            error = "장비 타입 또는 부위 설정을 확인하세요.";
        }
        return false;
    }

    public bool TryUseAt(int index, Player player, out string error)
    {
        error = null;
        if (!TryGetItem(index, out var item) || !(item is ConsumableItem consumable) || consumable.quantity <= 0)
            error = "사용할 소모품을 선택하세요.";
        else if (player == null || player.IsDead) error = "현재 아이템을 사용할 수 없습니다.";
        else if (item is PotionItem && player.CurrentHP >= player.MaxHP) error = "이미 체력이 가득 찼습니다.";
        else
        {
            int before = consumable.quantity;
            consumable.Use(player);
            if (consumable == null || consumable.quantity < before) return true;
            error = "이 아이템은 사용할 수 없습니다.";
        }
        return false;
    }

    public bool TryDiscardAt(int index, int quantity, Player player, out string error)
    {
        error = null;
        if (!TryGetItem(index, out var item)) { error = "선택한 아이템이 더 이상 없습니다."; return false; }
        int available = item is ConsumableItem stack ? stack.quantity : 1;
        if (quantity <= 0 || quantity > available) { error = "버릴 수량이 올바르지 않습니다."; return false; }
        if (item is ConsumableItem consumable && quantity < available)
        {
            consumable.quantity -= quantity;
            InventoryChanged?.Invoke();
            return true;
        }
        if (item is EquipmentItem equipment && IsEquipped(player, equipment)) player.ReleaseItem(equipment);
        RemoveFromInventory(item);
        return true;
    }

    public bool TryGetItem(int index, out BaseItem item)
    {
        item = index >= 0 && index < inventoryItems.Count ? inventoryItems[index] : null;
        return item != null;
    }

    public static bool IsEquipped(Player player, BaseItem item)
    {
        var gear = player != null ? player.equipmentData : null;
        if (gear == null || item == null) return false;
        if (gear.weaponItem == item || gear.accessoryItem == item) return true;
        if (gear.armorItem != null)
            foreach (var armor in gear.armorItem) if (armor != null && armor == item) return true;
        return false;
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
