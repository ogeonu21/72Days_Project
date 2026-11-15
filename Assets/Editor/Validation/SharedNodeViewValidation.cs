using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>임시 UI로 공유 선택지·구독·영역 복원을 검사한다. 실행 씬과 세이브는 변경하지 않는다.</summary>
public static class SharedNodeViewValidation
{
    [MenuItem("Tools/Validation/Shared Node View")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode 전용 검사입니다.");
        int passed = 0;
        Action<bool, string> check = (ok, label) =>
        {
            if (!ok) throw new InvalidOperationException(label);
            passed++;
        };
        var root = new GameObject("SharedNodeViewValidation", typeof(RectTransform), typeof(Canvas));
        root.hideFlags = HideFlags.HideAndDontSave;
        SharedNodeView view = null;
        try
        {
            root.SetActive(false);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1080, 1920);
            view = Child("Shared", root.transform).gameObject.AddComponent<SharedNodeView>();
            var viewport = Child("DialogueViewport", view.transform);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var dialogueScroll = viewport.gameObject.AddComponent<ScrollRect>();
            var text = Child("Text", viewport).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            if (text.font == null)
                text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (text.font == null) throw new InvalidOperationException("검사용 TMP 폰트가 없습니다.");
            text.fontSharedMaterial = text.font.material;
            text.text = "Shared text";
            text.fontSize = 48;
            viewport.offsetMin = new Vector2(80, 80);
            viewport.offsetMax = new Vector2(-80, -80);
            var choices = Child("Choices", view.transform);
            choices.anchorMax = new Vector2(1, 0);
            choices.offsetMin = new Vector2(80, 80);
            choices.offsetMax = new Vector2(-80, 720);
            var scroll = Child("Scroll", choices).gameObject.AddComponent<ScrollRect>();
            var content = Child("Content", scroll.transform);
            content.gameObject.AddComponent<VerticalLayoutGroup>();
            var template = Child("Template", view.transform).gameObject.AddComponent<Button>();
            ((RectTransform)template.transform).sizeDelta = new Vector2(800, 100);
            var label = Child("Label", template.transform).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = text.font;
            label.fontSharedMaterial = text.fontSharedMaterial;
            template.gameObject.SetActive(false);
            view.dialogueText = text; view.dialogueScroll = dialogueScroll; view.choiceArea = choices;
            dialogueScroll.content = text.rectTransform; dialogueScroll.viewport = viewport;
            view.choiceScroll = scroll; view.choiceContent = content; view.choiceTemplate = template;
            scroll.content = content;
            var first = Child("First", root.transform).gameObject.AddComponent<UIController>();
            var second = Child("Second", root.transform).gameObject.AddComponent<UIController>();
            first.sharedView = view; second.sharedView = view;
            first.gameObject.SetActive(false); second.gameObject.SetActive(false);
            root.SetActive(true);
            // 일반 MonoBehaviour의 Edit Mode 생명주기는 검사에서 명시적으로 구동한다.
            Lifecycle(view, "OnDisable");
            Lifecycle(view, "OnEnable");
            first.gameObject.SetActive(true);
            Lifecycle(first, "OnDisable");
            Lifecycle(first, "OnEnable");
            text.ForceMeshUpdate(true, true);
            label.ForceMeshUpdate(true, true);
            var field = typeof(GameEvent).GetField("NodeTextUpdate", BindingFlags.Static | BindingFlags.NonPublic);
            var subscribers = ((Delegate)field.GetValue(null))?.GetInvocationList();
            check(subscribers != null && subscribers.Count(x => ReferenceEquals(x.Target, view)) == 1,
                "공유 본문 이벤트 구독자 1개");
            check(!subscribers.Any(x => ReferenceEquals(x.Target, first)), "모드 컨트롤러는 중복 구독하지 않음");
            var originalMin = viewport.offsetMin;
            var originalMax = viewport.offsetMax;
            check(view.OpenChoices(first), "현재 모드 선택지 열기");
            view.NewContent(first);
            int clicks = 0;
            var button = view.AddChoice(first, "선택", () => clicks++);
            var oldClick = button.onClick;
            button.onClick.Invoke();
            check(clicks == 1, "현재 목록 클릭 실행");
            view.AddChoice(first, "조건 숨김", null).gameObject.SetActive(false);
            typeof(SharedNodeView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            check(viewport.offsetMin.y > originalMin.y && text.fontSize == 48,
                "선택지 표시 중 본문 영역만 축소, 글자 크기 유지");
            var originalContent = view.choiceContent;
            var originalScroll = view.choiceScroll;
            view.NewContent(first);
            oldClick.Invoke();
            check(clicks == 1 && content.childCount == 0, "이전 목록 정리와 오래된 클릭 무효화");
            check(view.choiceContent == originalContent && view.choiceScroll == originalScroll,
                "Scroll/Content 재생성 없음");
            text.text = new string('A', 2500);
            typeof(SharedNodeView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            string singleWordDiagnostic = DescribeScroll(dialogueScroll, text, viewport);
            float actualPreferred = text.GetPreferredValues(text.text,
                Mathf.Max(1, text.rectTransform.rect.width - text.margin.x - text.margin.z), Mathf.Infinity).y + 8;
            bool singleWordValid = actualPreferred <= viewport.rect.height ||
                (dialogueScroll.enabled && text.rectTransform.rect.height > viewport.rect.height);
            text.text = string.Join("\n", Enumerable.Repeat("Long dialogue line for scrolling.", 100));
            typeof(SharedNodeView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            check(singleWordValid && dialogueScroll.enabled && text.rectTransform.rect.height > viewport.rect.height,
                "강제 줄바꿈 긴 본문 스크롤: " + DescribeScroll(dialogueScroll, text, viewport) +
                " | 단일단어 결과: " + singleWordDiagnostic);
            text.rectTransform.anchoredPosition += new Vector2(0, 30);
            var scrolledPosition = text.rectTransform.anchoredPosition;
            typeof(SharedNodeView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            check(text.rectTransform.anchoredPosition == scrolledPosition, "변경 없는 프레임에서 본문 스크롤 위치 유지");
            view.HideChoices(first);
            check(!dialogueScroll.enabled && text.rectTransform.anchorMin == Vector2.zero && text.rectTransform.anchorMax == Vector2.one,
                "선택지 숨김 후 본문 스크롤과 콘텐츠 영역 복원");
            check(viewport.offsetMin == originalMin && viewport.offsetMax == originalMax,
                "선택지 숨김 후 본문 원래 영역 복원");
            first.gameObject.SetActive(false);
            Lifecycle(first, "OnDisable");
            second.gameObject.SetActive(true);
            Lifecycle(second, "OnDisable");
            Lifecycle(second, "OnEnable");
            check(!view.OpenChoices(first) && view.OpenChoices(second), "모드 전환 후 이전 소유자 접근 차단");
            view.NewContent(second);
            view.AddChoice(second, "새 모드", () => clicks++);
            view.HideChoices(first);
            check(choices.gameObject.activeSelf && content.childCount == 1, "이전 모드 정리가 새 목록을 숨기지 않음");
            oldClick.Invoke();
            check(clicks == 1, "모드 전환 후 이전 입력 실행 없음");
            second.gameObject.SetActive(false);
            Lifecycle(second, "OnDisable");
            check(!choices.gameObject.activeSelf && content.childCount == 0, "모드 종료 시 공통 선택지 정리");
            check(choices.GetComponentsInChildren<ScrollRect>(true).Length == 1, "공통 선택지 Scroll 1개 유지");
            return $"Shared Node View Edit Mode 검사 {passed}개 통과";
        }
        finally
        {
            if (view != null) Lifecycle(view, "OnDisable");
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static string DescribeScroll(ScrollRect scroll, TMP_Text text, RectTransform viewport)
    {
        float width = Mathf.Max(1, text.rectTransform.rect.width - text.margin.x - text.margin.z);
        float preferred = text.GetPreferredValues(text.text, width, Mathf.Infinity).y;
        return $"enabled={scroll.enabled}, contentHeight={text.rectTransform.rect.height}, viewportHeight={viewport.rect.height}, " +
            $"preferred={preferred}, width={width}, wrapping={text.enableWordWrapping}, fontSize={text.fontSize}, " +
            $"scrollActive={scroll.gameObject.activeInHierarchy}, textLength={text.text.Length}";
    }

    private static void Lifecycle(MonoBehaviour target, string method)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }

    private static RectTransform Child(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
}
