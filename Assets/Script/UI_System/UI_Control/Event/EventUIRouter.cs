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
        ShowOptions();
    }

    public void Hide()
    {
        foreach (var panel in panels.Values) if (panel != null) panel.SetActive(false);
        awaitingResult = false;
    }
    private void OnDisable() => Hide();

    private Transform NewContent()
    {
        foreach (Transform child in activePanel.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
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

    private void Button(Transform parent, string label, Action action)
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
            Button(content, label, () => Choose(captured));
        }
        // 이미 수령한 이벤트를 재방문해도 출구를 제공한다.
        if (node.definition.exitNode != null)
            Button(content, "떠나기", () => NodeManager.Instance.GoToNode(node.definition.exitNode));
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
