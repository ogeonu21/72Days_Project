using TMPro;
using UnityEngine;

/// <summary>기존 HUD 성향 영역에 72×72 아이콘을 배치한다. 매 프레임 조회하지 않는다.</summary>
[DisallowMultipleComponent]
public sealed class PlayerTendencyUI : MonoBehaviour
{
    private static readonly string[] IconNames =
        { "realist-strong", "realist", "neutral", "idealist", "idealist-strong" };
    private static readonly Sprite[] Icons = new Sprite[5];
    private Player player;
    private TMP_Text label;
    private UnityEngine.UI.Image icon;

    public static PlayerTendencyUI Ensure(TMP_Text target)
    {
        if (target == null) return null;
        var view = target.GetComponent<PlayerTendencyUI>();
        if (view == null) view = target.gameObject.AddComponent<PlayerTendencyUI>();
        view.Initialize(target);
        return view;
    }

    private void Initialize(TMP_Text target)
    {
        label = target;
        label.fontSize = 48;
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.margin = new Vector4(100, 0, 0, 0);
        label.raycastTarget = false;
        if (icon == null)
        {
            var child = new GameObject("TendencyIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            icon = child.GetComponent<UnityEngine.UI.Image>();
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(60, 0);
            rect.sizeDelta = new Vector2(80, 80);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }
        Refresh();
    }

    public void Bind(Player value)
    {
        Unsubscribe();
        player = value;
        if (isActiveAndEnabled && player != null) player.TendencyChanged += OnTendencyChanged;
        Refresh();
    }

    private void OnEnable()
    {
        Unsubscribe();
        if (player != null) player.TendencyChanged += OnTendencyChanged;
        Refresh();
    }

    private void OnDisable() { Unsubscribe(); }
    private void OnDestroy() { Unsubscribe(); }
    private void Unsubscribe()
    {
        if (player != null) player.TendencyChanged -= OnTendencyChanged;
    }
    private void OnTendencyChanged(int value) { Refresh(); }

    private void Refresh()
    {
        if (icon == null || label == null) return;
        // 플레이어 생성 전에는 이전 회차의 성향 표시를 남기지 않는다.
        int value = player != null ? player.tendency : 0;
        int index = TendencyRules.GetIconIndex(value);
        if (Icons[index] == null) Icons[index] = Resources.Load<Sprite>("UI/Tendency/" + IconNames[index]);
        icon.sprite = Icons[index];
        icon.enabled = icon.sprite != null;
        label.text = "성향";
        //label.text = TendencyRules.GetLabel(value);
    }
}
