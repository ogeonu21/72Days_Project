using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>노드 종류와 무관한 선택지 표시 기반. 실행 규칙과 화면 전환은 자식이 소유한다.</summary>
public abstract class ChoiceUIRouter : MonoBehaviour
{
    private readonly Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();
    protected GameObject activePanel;
    protected Button template;
    protected TMP_FontAsset font;
    private int contentVersion;
    protected virtual Vector2 PanelAnchorMin => new Vector2(.05f, .04f);
    protected virtual Vector2 PanelAnchorMax => new Vector2(.95f, .43f);
    protected virtual Transform ChoicePanelParent => transform;
    protected virtual void LayoutPanel(RectTransform rect)
    {
        rect.anchorMin = PanelAnchorMin;
        rect.anchorMax = PanelAnchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    protected virtual void OnDisable() => HideChoices();

    protected bool OpenChoices(string key, Button buttonTemplate, TMP_FontAsset textFont)
    {
        HideChoices();
        template = buttonTemplate;
        font = textFont;
        if (template == null || template.GetComponentInChildren<TMP_Text>(true) == null)
        {
            Debug.LogError("[ChoiceUI] TMP 라벨이 있는 버튼 템플릿이 필요합니다.", this);
            return false;
        }
        if (!panels.TryGetValue(key, out activePanel) || activePanel == null)
        {
            activePanel = new GameObject(key + "ChoicePanel", typeof(RectTransform));
            activePanel.transform.SetParent(ChoicePanelParent, false);
            var rect = (RectTransform)activePanel.transform;
            LayoutPanel(rect);
            panels[key] = activePanel;
        }
        activePanel.SetActive(true);
        LayoutPanel((RectTransform)activePanel.transform);
        activePanel.transform.SetAsLastSibling();
        return true;
    }

    protected virtual void OnDestroy()
    {
        // 부모 밖에 배치한 패널도 라우터 수명과 함께 정리한다.
        foreach (var panel in panels.Values)
        {
            if (panel == null) continue;
            if (Application.isPlaying) Destroy(panel); else DestroyImmediate(panel);
        }
        panels.Clear();
    }

    protected void HideChoices()
    {
        contentVersion++;
        foreach (var panel in panels.Values) if (panel != null) panel.SetActive(false);
    }

    protected Transform NewContent()
    {
        contentVersion++;
        for (int i = activePanel.transform.childCount - 1; i >= 0; i--)
        {
            var child = activePanel.transform.GetChild(i).gameObject;
            child.SetActive(false);
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        activePanel.SetActive(true);
        var viewport = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.transform.SetParent(activePanel.transform, false);
        var rect = (RectTransform)viewport.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12, 12); rect.offsetMax = new Vector2(-12, -12);
        viewport.GetComponent<Image>().color = Color.clear;
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(rect, false);
        var cr = (RectTransform)content.transform;
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = Vector2.one; cr.pivot = new Vector2(.5f, 1);
        cr.offsetMin = cr.offsetMax = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12; layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = rect; scroll.content = cr; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return cr;
    }

    protected Button AddChoice(Transform parent, string label, Action action, bool available = true)
    {
        var button = Instantiate(template, parent);
        button.name = "ChoiceAction";
        button.onClick = new Button.ButtonClickedEvent();
        int version = contentVersion;
        button.onClick.AddListener(() =>
        {
            // 삭제 대기 중인 이전 목록이나 숨겨진 화면의 입력은 무시한다.
            if (version == contentVersion && isActiveAndEnabled && button != null && button.isActiveAndEnabled && button.interactable)
                action?.Invoke();
        });
        button.gameObject.SetActive(true);
        var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = 95;
        var text = button.GetComponentInChildren<TMP_Text>(true);
        text.text = label; if (font != null) text.font = font;
        text.enableAutoSizing = true; text.fontSizeMin = text.fontSizeMax = 48;
        button.interactable = available;
        return button;
    }
}
