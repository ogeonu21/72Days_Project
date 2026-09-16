using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventorySlotUI : MonoBehaviour
{
    public Button button;
    public AddressableItemIcon icon;
    public TMP_Text quantityText;
    public TMP_Text nameText;
    public TMP_Text equippedText;
    public Image selection;
    private Action clicked;
    public BaseItem Item { get; private set; }

    private void Awake() { button.onClick.AddListener(OnClick); }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(OnClick); }
    private void OnClick() => clicked?.Invoke();

    public void Bind(BaseItem item, bool equipped, bool selected, Action onClick, string emptyLabel = "")
    {
        Item = item;
        clicked = onClick;
        button.interactable = item != null;
        icon.Bind(item != null ? item.itemIcon : "");
        nameText.text = item != null ? item.itemName : emptyLabel;
        quantityText.text = item is ConsumableItem consumable ? consumable.quantity.ToString() : "";
        equippedText.text = equipped && item != null ? "장착" : "";
        selection.enabled = selected && item != null;
    }
}
