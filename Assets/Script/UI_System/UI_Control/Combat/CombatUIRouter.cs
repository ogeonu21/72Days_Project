using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>전투 행동과 하위 선택지만 표시한다. 턴 소비와 판정은 CombatManager가 담당한다.</summary>
public class CombatUIRouter : ChoiceUIRouter
{
    [SerializeField] private Button choiceTemplate;
    private CombatManager combatManager;
    private InventoryManager inventory;
    private Player player;
    private bool showingItems;
    private Button[] attackAreaButtons = System.Array.Empty<Button>();
    private TMP_Text dialogueText;
    private string layoutText;
    private Vector2 layoutParentSize;
    private Vector2 originalAnchorMin, originalAnchorMax, originalOffsetMin, originalOffsetMax;
    private float originalFontSize;
    private bool hasOriginalTextLayout;
    protected override Transform ChoicePanelParent => transform.parent != null ? transform.parent : transform;

    protected override void LayoutPanel(RectTransform rect)
    {
        if (!rect.gameObject.activeSelf) return;
        var parentRect = ChoicePanelParent as RectTransform;
        var attackRect = transform as RectTransform;
        if (parentRect == null || attackRect == null || parentRect == attackRect) { base.LayoutPanel(rect); return; }
        var corners = new Vector3[4];
        attackRect.GetWorldCorners(corners);
        float attackBottom = Mathf.Clamp(parentRect.InverseTransformPoint(corners[0]).y - parentRect.rect.yMin, 0, parentRect.rect.height);
        float available = Mathf.Max(0, attackBottom - 24);
        float textHeight = 0;
        if (dialogueText != null && !string.IsNullOrWhiteSpace(dialogueText.text))
        {
            float width = Mathf.Max(1, dialogueText.rectTransform.rect.width - dialogueText.margin.x - dialogueText.margin.z);
            float preferred = dialogueText.GetPreferredValues(dialogueText.text, width, Mathf.Infinity).y;
            // 최소 한 버튼과 스크롤 안쪽 여백은 남긴다. 짧은 본문은 필요한 높이만 사용한다.
            textHeight = Mathf.Min(preferred + 8, Mathf.Max(0, available - 124 - 16));
        }
        float gap = textHeight > 0 ? 16 : 0;
        float choicesTop = Mathf.Min(parentRect.rect.height, 12 + Mathf.Max(0, available - textHeight - gap));
        rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0);
        rect.offsetMin = new Vector2(12, 12);
        rect.offsetMax = new Vector2(-12, choicesTop);
        if (dialogueText != null)
        {
            var textRect = dialogueText.rectTransform;
            var textParent = textRect.parent as RectTransform;
            if (textParent == null) return;
            float bottom = textParent.InverseTransformPoint(parentRect.TransformPoint(new Vector3(0, parentRect.rect.yMin + choicesTop + gap, 0))).y - textParent.rect.yMin;
            float top = textParent.InverseTransformPoint(parentRect.TransformPoint(new Vector3(0, parentRect.rect.yMin + choicesTop + gap + textHeight, 0))).y - textParent.rect.yMin;
            textRect.anchorMin = new Vector2(textRect.anchorMin.x, 0);
            textRect.anchorMax = new Vector2(textRect.anchorMax.x, 0);
            textRect.offsetMin = new Vector2(textRect.offsetMin.x, bottom);
            textRect.offsetMax = new Vector2(textRect.offsetMax.x, Mathf.Max(bottom, top));
        }
        layoutText = dialogueText != null ? dialogueText.text : null;
        layoutParentSize = parentRect.rect.size;
    }

    private void LateUpdate()
    {
        if (activePanel == null || !activePanel.activeSelf) return;
        var parent = ChoicePanelParent as RectTransform;
        if (parent != null && (layoutParentSize != parent.rect.size ||
            layoutText != (dialogueText != null ? dialogueText.text : null)))
            LayoutPanel((RectTransform)activePanel.transform);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (activePanel != null && activePanel.activeSelf) LayoutPanel((RectTransform)activePanel.transform);
    }

    protected override void HideChoices()
    {
        base.HideChoices();
        RestoreTextLayout();
    }

    private void RestoreTextLayout()
    {
        if (!hasOriginalTextLayout || dialogueText == null) return;
        var rect = dialogueText.rectTransform;
        rect.anchorMin = originalAnchorMin;
        rect.anchorMax = originalAnchorMax;
        rect.offsetMin = originalOffsetMin;
        rect.offsetMax = originalOffsetMax;
        dialogueText.enableAutoSizing = false;
        dialogueText.fontSize = originalFontSize;
        layoutText = null;
    }

    public void Configure(Button buttonTemplate, TMP_FontAsset textFont, Button[] bodyButtons = null, TMP_Text bodyText = null)
    {
        Unbind();
        RestoreTextLayout();
        if (dialogueText != bodyText) hasOriginalTextLayout = false;
        dialogueText = bodyText;
        if (dialogueText != null)
        {
            if (!hasOriginalTextLayout)
            {
                var rect = dialogueText.rectTransform;
                originalAnchorMin = rect.anchorMin; originalAnchorMax = rect.anchorMax;
                originalOffsetMin = rect.offsetMin; originalOffsetMax = rect.offsetMax;
                originalFontSize = dialogueText.fontSize;
                hasOriginalTextLayout = true;
            }
            dialogueText.enableAutoSizing = false;
            dialogueText.overflowMode = TextOverflowModes.Masking;
        }
        attackAreaButtons = bodyButtons ?? System.Array.Empty<Button>();
        foreach (var button in attackAreaButtons) if (button != null) button.gameObject.SetActive(true);
        choiceTemplate = buttonTemplate;
        font = textFont;
        combatManager = CombatManager.Instance;
        inventory = InventoryManager.Instance;
        if (combatManager != null) combatManager.ActionSelectionChanged += OnSelectionChanged;
        if (inventory != null) inventory.InventoryChanged += OnInventoryChanged;
        OnSelectionChanged(combatManager != null && combatManager.CanChooseAction);
    }

    protected override void OnDisable()
    {
        Unbind();
        HideChoices();
        SetAttackAvailability(false);
        player = null;
    }

    private void Unbind()
    {
        if (combatManager != null) combatManager.ActionSelectionChanged -= OnSelectionChanged;
        if (inventory != null) inventory.InventoryChanged -= OnInventoryChanged;
    }

    private void OnSelectionChanged(bool available)
    {
        SetAttackAvailability(available);
        if (!available) { HideChoices(); return; }
        player = CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null;
        if (OpenChoices("Combat", choiceTemplate, font)) ShowActions();
    }

    private void SetAttackAvailability(bool available)
    {
        foreach (var button in attackAreaButtons) if (button != null) button.interactable = available;
    }

    private void OnInventoryChanged()
    {
        if (showingItems && combatManager != null && combatManager.CanChooseAction) ShowItems();
    }

    public void ShowActions()
    {
        if (!CanSelect()) return;
        showingItems = false;
        var content = NewContent();
        AddChoice(content, "아이템 사용", ShowItems);
        AddChoice(content, combatManager.CanEscape
            ? $"도주 ({combatManager.EscapeProbability:P0})" : "도주 불가 · 이동 노드 없음",
            () => Submit(new PlayerInputData(PlayerBehaviour.Run, default, null)), combatManager.CanEscape);
    }

    private bool CanSelect() => activePanel != null && player != null && combatManager != null && combatManager.CanChooseAction;

    public void ShowItems()
    {
        if (!CanSelect()) return;
        showingItems = true;
        var content = NewContent();
        int count = 0;
        if (inventory != null)
            foreach (var item in inventory.inventoryItems)
            {
                if (!(item is ConsumableItem consumable) || consumable.quantity <= 0) continue;
                BaseItem selected = item;
                bool usable = !(item is PotionItem) || player.CurrentHP < player.MaxHP;
                AddChoice(content, $"{item.itemName} ×{consumable.quantity}" + (usable ? "" : " · 체력 가득 참"),
                    () => Submit(new PlayerInputData(PlayerBehaviour.Use, default, selected)), usable);
                count++;
            }
        if (count == 0) AddChoice(content, "사용할 소모품이 없습니다.", null, false);
        AddChoice(content, "뒤로", ShowActions);
    }

    // 기존 Inspector의 정수 인자 연결과 호환된다.
    public void AttackAreaInput(int serializedArea)
    {
        if (combatManager == null) combatManager = CombatManager.Instance;
        if (player == null && CharacterManager.Instance != null) player = CharacterManager.Instance.currentPlayer;
        if (combatManager == null || !combatManager.CanChooseAction || player == null) return;
        if (!System.Enum.IsDefined(typeof(AttackArea), serializedArea) ||
            player.areaDataDB == null || serializedArea >= player.areaDataDB.Length) return;
        Submit(new PlayerInputData(PlayerBehaviour.Attack, player.areaDataDB[serializedArea], null));
    }

    private void Submit(PlayerInputData input)
    {
        if (combatManager == null) return;
        if (combatManager.TrySubmitInput(input, out string error)) HideChoices();
        else
        {
            Debug.LogWarning("[CombatUI] " + error);
            if (showingItems) ShowItems(); else ShowActions();
        }
    }
}
