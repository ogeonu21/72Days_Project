using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>이벤트 종류별 패널의 표시만 담당한다. 지급과 이동은 이벤트 실행부에 위임한다.</summary>
public sealed class EventUIRouter : MonoBehaviour
{
    private readonly Dictionary<EventKind, GameObject> panels = new Dictionary<EventKind, GameObject>();
    private EventNode node;
    private Button template;
    private TMP_FontAsset font;
    private GameObject activePanel;
    private bool awaitingResult;
    private readonly List<(EventOption option, Button button)> optionButtons = new List<(EventOption, Button)>();
    private Player observedPlayer;
    private InventoryManager observedInventory;
    private EventManager observedEvents;

    public void Present(EventNode value, Button buttonTemplate, TMP_FontAsset textFont)
    {
        Hide();
        node = value; template = buttonTemplate; font = textFont;
        if (node == null || template == null) return;
        if (!panels.TryGetValue(node.definition.kind, out activePanel))
        {
            activePanel = new GameObject(node.definition.kind + "EventPanel", typeof(RectTransform), typeof(Image));
            activePanel.transform.SetParent(transform, false);
            var rect = (RectTransform)activePanel.transform;
            rect.anchorMin = new Vector2(.05f, .04f); rect.anchorMax = new Vector2(.95f, .43f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            activePanel.GetComponent<Image>().color = new Color(.07f, .09f, .12f, .98f);
            panels.Add(node.definition.kind, activePanel);
        }
        activePanel.SetActive(true);
        activePanel.transform.SetAsLastSibling();
        observedPlayer = CharacterManager.Instance != null ? CharacterManager.Instance.currentPlayer : null;
        observedInventory = InventoryManager.Instance;
        observedEvents = EventManager.Instance;
        if (observedPlayer != null) observedPlayer.TendencyChanged += OnTendencyChanged;
        if (observedInventory != null) observedInventory.InventoryChanged += RefreshAvailability;
        if (observedEvents != null) observedEvents.ProgressChanged += RefreshAvailability;
        PlayerEvent.onStatsChanged += RefreshAvailability;
        CurrencyEvent.OnCurrencyChanged += OnCurrencyChanged;
        ShowOptions();
    }

    public void Hide()
    {
        if (observedPlayer != null) observedPlayer.TendencyChanged -= OnTendencyChanged;
        if (observedInventory != null) observedInventory.InventoryChanged -= RefreshAvailability;
        if (observedEvents != null) observedEvents.ProgressChanged -= RefreshAvailability;
        PlayerEvent.onStatsChanged -= RefreshAvailability;
        CurrencyEvent.OnCurrencyChanged -= OnCurrencyChanged;
        observedPlayer = null; observedInventory = null; observedEvents = null;
        optionButtons.Clear();
        foreach (var panel in panels.Values) if (panel != null) panel.SetActive(false);
        awaitingResult = false;
    }
    private void OnDisable() => Hide();

    private Transform NewContent()
    {
        optionButtons.Clear();
        for (int i = activePanel.transform.childCount - 1; i >= 0; i--)
        {
            var child = activePanel.transform.GetChild(i);
            child.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        scrollObject.transform.SetParent(activePanel.transform, false);
        var rect = (RectTransform)scrollObject.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12, 12); rect.offsetMax = new Vector2(-12, -12);
        scrollObject.GetComponent<Image>().color = new Color(0, 0, 0, .01f);
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(rect, false);
        var cr = (RectTransform)content.transform;
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = Vector2.one; cr.pivot = new Vector2(.5f, 1);
        cr.offsetMin = cr.offsetMax = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12; layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = rect; scroll.content = cr; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return cr;
    }

    private void Label(Transform parent, string value)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = 30; text.text = value; text.raycastTarget = false;
    }

    private Button Button(Transform parent, string label, Action action)
    {
        var button = Instantiate(template, parent);
        button.name = "EventAction";
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => action());
        button.gameObject.SetActive(true);
        var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = 95;
        var text = button.GetComponentInChildren<TMP_Text>(true);
        text.text = label; text.font = font; text.enableAutoSizing = true; text.fontSizeMin = 20; text.fontSizeMax = 32;
        button.interactable = true;
        return button;
    }

    private void ShowOptions()
    {
        awaitingResult = false;
        var content = NewContent();
        Label(content, string.IsNullOrWhiteSpace(node.definition.title) ? node.definition.kind.ToString() : node.definition.title);
        foreach (var option in node.definition.options)
        {
            var captured = option;
            string label = option.text + (option.goldCost > 0 ? $"  ·  {option.goldCost} 골드" : "");
            var button = Button(content, label, () => Choose(captured));
            button.name = "EventAction_" + option.id;
            optionButtons.Add((option, button));
        }
        // 이미 수령한 이벤트를 재방문해도 출구를 제공한다.
        if (node.definition.exitNode != null)
            Button(content, "떠나기", () => NodeManager.Instance.GoToNode(node.definition.exitNode));
        RefreshAvailability();
    }

    private void OnTendencyChanged(int value) => RefreshAvailability();
    private void OnCurrencyChanged(CurrencyData value) => RefreshAvailability();

    public void RefreshAvailability()
    {
        if (awaitingResult || activePanel == null || !activePanel.activeSelf) return;
        foreach (var entry in optionButtons)
        {
            string reason = observedEvents != null ? observedEvents.GetUnavailableReason(node, entry.option) : "이벤트 시스템을 불러오는 중입니다.";
            entry.button.interactable = reason == null;
            var text = entry.button.GetComponentInChildren<TMP_Text>(true);
            text.text = entry.option.text + (entry.option.goldCost > 0 ? $" · 비용 {entry.option.goldCost} 골드" : "")
                + (reason == null ? "" : "\n[잠김] " + reason);
            text.color = reason == null ? Color.white : new Color(.65f, .65f, .65f);
            // 여러 조건의 미충족 사유가 잘리지 않도록 버튼 높이를 늘린다.
            var layout = entry.button.GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = Mathf.Max(95, 48 + text.text.Split('\n').Length * 38);
        }
    }

    private void Choose(EventOption option)
    {
        if (awaitingResult) return;
        awaitingResult = true;
        var result = EventManager.Instance.Execute(node, option);
        var content = NewContent();
        Label(content, result.message);
        Button(content, "확인", () =>
        {
            if (result.success && option.nextNode != null) NodeManager.Instance.GoToNode(option.nextNode);
            else ShowOptions();
        });
    }
}
