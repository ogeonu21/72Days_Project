using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>이름표 오른쪽에 효과와 남은 턴을 표시한다. 상태는 Character만 소유한다.</summary>
[DisallowMultipleComponent]
public sealed class CharacterEffectsUI : MonoBehaviour
{
    private Character source;
    private TMP_Text nameLabel;
    private Vector4 originalMargin;
    private bool originalWrapping;
    private TextOverflowModes originalOverflow;
    private RectTransform row;
    private readonly GameObject[] badges = new GameObject[3];
    private readonly TMP_Text[] turns = new TMP_Text[3];
    private static readonly string[] Labels = { "공격력 감소", "회피율 감소", "출혈" };

    public void Bind(Character character, TMP_Text label)
    {
        if (source != null) source.EffectsChanged -= Refresh;
        source = character;
        if (nameLabel == null)
        {
            nameLabel = label;
            originalMargin = label.margin;
            originalWrapping = label.enableWordWrapping;
            originalOverflow = label.overflowMode;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }
        if (row == null) Build();
        if (isActiveAndEnabled && source != null) source.EffectsChanged += Refresh;
        Refresh();
    }

    private void OnEnable()
    {
        if (source != null) { source.EffectsChanged -= Refresh; source.EffectsChanged += Refresh; }
        Refresh();
    }
    private void OnDisable()
    {
        if (source != null) source.EffectsChanged -= Refresh;
    }
    private void OnDestroy()
    {
        if (source != null) source.EffectsChanged -= Refresh;
        if (nameLabel != null)
        {
            nameLabel.margin = originalMargin;
            nameLabel.enableWordWrapping = originalWrapping;
            nameLabel.overflowMode = originalOverflow;
        }
        if (row != null)
        {
            if (Application.isPlaying) Destroy(row.gameObject);
            else DestroyImmediate(row.gameObject);
        }
    }

    private void Build()
    {
        row = new GameObject("StatusEffects", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(nameLabel.transform, false);
        row.anchorMin = row.anchorMax = new Vector2(1, .5f);
        row.pivot = new Vector2(1, .5f);
        row.sizeDelta = new Vector2(252, 40);
        for (int i = 0; i < badges.Length; i++)
        {
            var badge = new GameObject(Labels[i], typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(row, false);
            badges[i] = badge;
            var rect = (RectTransform)badge.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.pivot = new Vector2(0, .5f);
            rect.sizeDelta = new Vector2(80, 60);
            var background = badge.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0f);
            background.raycastTarget = false;
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(CharacterEffectIcon)).GetComponent<CharacterEffectIcon>();
            icon.transform.SetParent(rect, false);
            icon.rectTransform.anchorMin = new Vector2(0, 0);
            icon.rectTransform.anchorMax = new Vector2(.5f, 1);
            icon.rectTransform.offsetMin = new Vector2(3, 4);
            icon.rectTransform.offsetMax = new Vector2(-1, -4);
            icon.Effect = (CharacterEffect)i;
            icon.color = Color.white;
            icon.raycastTarget = false;
            var text = new GameObject("Turns", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            text.transform.SetParent(rect, false);
            text.rectTransform.anchorMin = new Vector2(.5f, 0);
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = new Vector2(-2, 0);
            text.font = nameLabel.font;
            text.fontSize = 32;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            turns[i] = text;
        }
    }

    public void Refresh()
    {
        if (row == null || nameLabel == null) return;
        int count = 0;
        for (int i = 0; i < badges.Length; i++)
        {
            int remaining = source != null ? source.GetEffectTurns((CharacterEffect)i) : 0;
            badges[i].SetActive(remaining > 0);
            if (remaining <= 0) continue;
            ((RectTransform)badges[i].transform).anchoredPosition = new Vector2(count++ * 72, 0);
            turns[i].text = "x" + remaining;
        }
        row.sizeDelta = new Vector2(count * 72, 40);
        // 긴 이름과 아이콘이 겹치지 않도록 실제 표시 개수만큼 이름 영역을 확보한다.
        nameLabel.margin = originalMargin + new Vector4(0, 0, count > 0 ? count * 72 + 8 : 0, 0);
    }
}
