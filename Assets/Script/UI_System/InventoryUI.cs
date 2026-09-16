using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>표시 상태만 소유한다. 아이템 변경은 InventoryManager/Player에 위임한다.</summary>
public sealed class InventoryUI : MonoBehaviour
{
    public InventoryManager inventory;
    public CharacterManager characters;
    public UIManager uiManager;
    public InventorySlotUI[] slots;
    // Helmet, Chestplate, Boots, Gloves, Weapon, Accessory 순서
    public InventorySlotUI[] equipmentSlots;
    public TMP_Text capacityText;
    public TMP_Text emptyText;
    public GameObject detailPanel;
    public AddressableItemIcon detailIcon;
    public TMP_Text itemNameText;
    public TMP_Text descriptionText;
    public TMP_Text statsText;
    public TMP_Text feedbackText;
    public Button actionButton;
    public TMP_Text actionLabel;
    public Button discardButton;
    public GameObject confirmPanel;
    public TMP_Text confirmText;
    public TMP_Text discardQuantityText;
    public Button minusButton;
    public Button plusButton;
    public Button allButton;
    public Button confirmButton;
    private Player player;
    private BaseItem selectedItem;
    private int selectedIndex = -1;
    private int discardQuantity = 1;
    private BaseItem pendingDiscard;
    public BaseItem SelectedItem => selectedItem;

    private void OnEnable()
    {
        // CharacterManager는 시작 씬의 전역 객체이므로 씬 간 직렬화 참조를 만들지 않는다.
        if (characters == null) characters = CharacterManager.Instance;
        inventory.InventoryChanged += Refresh;
        characters.OnCharacterReady += OnCharacterReady;
        SetPlayer(characters.currentPlayer);
        ClearSelection();
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.InventoryChanged -= Refresh;
        if (characters != null) characters.OnCharacterReady -= OnCharacterReady;
        SetPlayer(null);
        ClearSelection();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (confirmPanel.activeSelf) CancelDiscard();
        else if (detailPanel.activeSelf) CloseDetails();
        else uiManager.CloseUI(gameObject);
    }

    private void OnCharacterReady(Player newPlayer, Enemy enemy) { SetPlayer(newPlayer); Refresh(); }
    private void SetPlayer(Player value)
    {
        if (player != null) player.StatsChanged -= Refresh;
        player = value;
        if (player != null) player.StatsChanged += Refresh;
    }

    public void Refresh()
    {
        if (!isActiveAndEnabled || inventory == null) return;
        capacityText.text = $"소지품  {inventory.inventoryItems.Count} / {inventory.Capacity}";
        emptyText.gameObject.SetActive(inventory.inventoryItems.Count == 0);
        if (detailPanel.activeSelf && selectedItem == null) ClearSelection();
        if (selectedItem != null && selectedIndex >= 0 &&
            (!inventory.TryGetItem(selectedIndex, out var current) || current != selectedItem)) ClearSelection();
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            inventory.TryGetItem(i, out var item);
            slots[i].Bind(item, InventoryManager.IsEquipped(player, item), item != null && i == selectedIndex,
                () => SelectInventorySlot(index));
        }
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            int slot = i;
            BaseItem item = GetEquipment(i);
            equipmentSlots[i].Bind(item, false, item != null && selectedIndex < 0 && item == selectedItem,
                () => SelectEquipmentSlot(slot), EquipmentLabel(i));
        }
        if (selectedItem != null)
        {
            if (selectedIndex < 0 && !InventoryManager.IsEquipped(player, selectedItem)) ClearSelection();
            else ShowDetails();
        }
        if (confirmPanel.activeSelf)
        {
            if (pendingDiscard != selectedItem || selectedItem == null) CancelDiscard();
            else UpdateDiscardQuantity();
        }
    }

    public void SelectInventorySlot(int index)
    {
        if (!inventory.TryGetItem(index, out var item)) return;
        selectedIndex = index;
        selectedItem = item;
        feedbackText.text = "";
        CancelDiscard();
        detailPanel.SetActive(true);
        Refresh();
        EventSystem.current?.SetSelectedGameObject(actionButton.interactable ? actionButton.gameObject : discardButton.gameObject);
    }

    public void SelectEquipmentSlot(int index)
    {
        var item = GetEquipment(index);
        if (item == null) return;
        int inInventory = inventory.inventoryItems.IndexOf(item);
        if (inInventory >= 0) { SelectInventorySlot(inInventory); return; }
        selectedIndex = -1;
        selectedItem = item;
        feedbackText.text = "";
        CancelDiscard();
        detailPanel.SetActive(true);
        Refresh();
    }

    private void ShowDetails()
    {
        detailIcon.Bind(selectedItem.itemIcon);
        itemNameText.text = selectedItem.itemName;
        descriptionText.text = selectedItem.itemDescription;
        var builder = new StringBuilder();
        if (selectedItem is WeaponItem weapon) builder.Append($"무기\n공격력 +{weapon.attackBonus}  /  사거리 {weapon.range}");
        else if (selectedItem is ArmorItem armor) builder.Append($"{EquipmentLabel((int)armor.armorType)}\n최대 체력 +{armor.hpBonus}  /  회피 +{armor.dodgeBonus:P0}");
        else if (selectedItem is AccessoryItem accessory) builder.Append($"장신구\n회피 +{accessory.dodgeBonus:P0}");
        else if (selectedItem is PotionItem potion) builder.Append($"소모품\n체력 {potion.health} 회복");
        if (selectedItem is EquipmentItem gear) builder.Append($"\n내구도 {gear.durability}");
        if (selectedItem is ConsumableItem stack) builder.Append($"\n보유 수량 {stack.quantity}");
        builder.Append($"\n가치 {selectedItem.itemValue} 골드");
        bool equipped = InventoryManager.IsEquipped(player, selectedItem);
        if (equipped) builder.Append("\n현재 장착 중");
        statsText.text = builder.ToString();
        actionLabel.text = selectedItem is EquipmentItem ? (equipped ? "장착 해제" : "장착") : "사용";
        actionButton.interactable = player != null && !player.IsDead &&
            (selectedItem is EquipmentItem equipment ? equipment.durability > 0 || equipped :
             selectedItem is PotionItem consumable && consumable.isConsumable && consumable.quantity > 0 && player.CurrentHP < player.MaxHP);
        discardButton.interactable = selectedIndex >= 0;
    }

    public void ActivateSelected()
    {
        if (selectedItem == null || player == null || player.IsDead) return;
        string error = null;
        if (selectedItem is EquipmentItem equipment && InventoryManager.IsEquipped(player, selectedItem))
        {
            // 구 저장에서 인벤토리에 없던 장비는 해제 전에 소지품으로 반환한다.
            if (selectedIndex < 0)
            {
                if (!inventory.CanAdd(equipment)) { feedbackText.text = "장비를 돌려놓을 빈 슬롯이 필요합니다."; return; }
                inventory.AddToInventory(equipment);
                selectedIndex = inventory.inventoryItems.IndexOf(equipment);
            }
            player.ReleaseItem(equipment);
        }
        else if (selectedItem is EquipmentItem) inventory.TryEquipAt(selectedIndex, player, out error);
        else inventory.TryUseAt(selectedIndex, player, out error);
        feedbackText.text = error ?? "";
        Refresh();
    }

    public void BeginDiscard()
    {
        if (selectedItem == null || selectedIndex < 0) return;
        pendingDiscard = selectedItem;
        discardQuantity = 1;
        confirmPanel.SetActive(true);
        confirmText.text = $"{selectedItem.itemName}\n정말 버리시겠습니까?" +
            (InventoryManager.IsEquipped(player, selectedItem) ? "\n장착 중인 장비는 해제됩니다." : "");
        UpdateDiscardQuantity();
    }

    private int AvailableQuantity => selectedItem is ConsumableItem stack ? stack.quantity : 1;
    public void IncreaseDiscard() { if (discardQuantity < AvailableQuantity) discardQuantity++; UpdateDiscardQuantity(); }
    public void DecreaseDiscard() { discardQuantity = Mathf.Max(1, discardQuantity - 1); UpdateDiscardQuantity(); }
    public void DiscardAll() { discardQuantity = AvailableQuantity; UpdateDiscardQuantity(); }
    private void UpdateDiscardQuantity()
    {
        discardQuantity = Mathf.Clamp(discardQuantity, 1, Mathf.Max(1, AvailableQuantity));
        discardQuantityText.text = $"{discardQuantity} / {AvailableQuantity}개";
        minusButton.interactable = discardQuantity > 1;
        plusButton.interactable = discardQuantity < AvailableQuantity;
        allButton.interactable = discardQuantity < AvailableQuantity;
    }

    public void ConfirmDiscard()
    {
        if (pendingDiscard == null || pendingDiscard != selectedItem ||
            !inventory.TryGetItem(selectedIndex, out var current) || current != pendingDiscard) { CancelDiscard(); return; }
        inventory.TryDiscardAt(selectedIndex, discardQuantity, player, out var error);
        CancelDiscard();
        feedbackText.text = error ?? "";
        Refresh();
    }
    public void CancelDiscard() { pendingDiscard = null; confirmPanel.SetActive(false); }
    public void CloseDetails() { ClearSelection(); Refresh(); }
    private void ClearSelection()
    {
        selectedItem = null;
        selectedIndex = -1;
        if (detailPanel != null) detailPanel.SetActive(false);
        if (confirmPanel != null) CancelDiscard();
    }

    private BaseItem GetEquipment(int index)
    {
        var gear = player != null ? player.equipmentData : null;
        if (gear == null) return null;
        if (index == 4) return gear.weaponItem;
        if (index == 5) return gear.accessoryItem;
        return gear.armorItem != null && index >= 0 && index < gear.armorItem.Length ? gear.armorItem[index] : null;
    }
    private static string EquipmentLabel(int index)
    {
        switch (index) { case 0: return "머리"; case 1: return "몸통"; case 2: return "신발"; case 3: return "장갑"; case 4: return "무기"; default: return "장신구"; }
    }
}
