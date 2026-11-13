using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MobileUiValidation
{
    [MenuItem("Tools/Mobile UI/Add Safe Area to Selected Canvas")]
    private static void AddSafeArea()
    {
        var selected = Selection.activeGameObject;
        var canvas = selected != null ? selected.GetComponent<Canvas>() : null;
        if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
        {
            Debug.LogWarning("Hierarchy에서 루트 화면 Canvas를 선택하세요.");
            return;
        }
        if (canvas.GetComponent<CanvasSafeArea>() == null) Undo.AddComponent<CanvasSafeArea>(selected);
    }

    [MenuItem("Tools/Mobile UI/Run Safe Area Math Checks")]
    public static void RunChecks()
    {
        var viewport = new Rect(0, 0, 1080, 1920);
        Check(CanvasSafeArea.NormalizeSafeArea(viewport, viewport), new Rect(0, 0, 1, 1), "전체 화면");
        Check(CanvasSafeArea.NormalizeSafeArea(new Rect(0, 96, 1080, 1728), viewport), new Rect(0, .05f, 1, .9f), "상하 인셋");
        Check(CanvasSafeArea.NormalizeSafeArea(new Rect(0, 50, 1080, 2300), new Rect(0, 240, 1080, 1920)), new Rect(0, 0, 1, 1), "레터박스 교집합");
        Check(CanvasSafeArea.NormalizeSafeArea(viewport, new Rect()), new Rect(0, 0, 1, 1), "초기 해상도 없음");
        Debug.Log("[MobileUiValidation] 안전 영역 계산 4/4 통과. 실제 기기 검증과는 별개입니다.");
    }

    private static void Check(Rect actual, Rect expected, string label)
    {
        if (Vector2.Distance(actual.min, expected.min) > .0001f || Vector2.Distance(actual.max, expected.max) > .0001f)
            throw new InvalidOperationException(label + ": " + actual + " != " + expected);
    }

    // LoadingSceneController를 끈 격리 Play Mode에서 표시가 안정화된 뒤 호출한다.
    // 버튼을 실행하지 않으므로 원격 조회·오류 전송을 발생시키지 않는다.
    public static string RunLoadingLayoutChecks()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("격리 Play Mode에서 실행하세요.");
        var controller = UnityEngine.Object.FindObjectOfType<LoadingSceneController>();
        if (controller == null || controller.enabled)
            throw new InvalidOperationException("LoadingSceneController를 비활성화한 격리 LoadingScene이 필요합니다.");
        if (EventSystem.current == null) throw new InvalidOperationException("EventSystem이 없습니다.");
        int passed = 0;
        Action<bool, string> check = (ok, label) =>
        {
            if (!ok) throw new InvalidOperationException(label);
            passed++;
        };
        check(IsOnScreen(controller.loadingText.rectTransform), "로딩 문구 화면 이탈");
        check(!controller.loadingText.isTextOverflowing, "로딩 문구 잘림");
        check(controller.dialog.activeInHierarchy, "검사할 모달이 표시되지 않았습니다.");
        check(IsOnScreen((RectTransform)controller.dialog.transform), "모달 화면 이탈");
        check(IsOnScreen(controller.messageText.rectTransform), "모달 본문 영역 화면 이탈");
        check(!controller.messageText.isTextOverflowing, "모달 본문 잘림");
        foreach (var button in new[] { controller.confirmButton, controller.retryButton, controller.reportButton })
        {
            if (!button.gameObject.activeInHierarchy) continue;
            check(IsOnScreen((RectTransform)button.transform), button.name + " 화면 이탈");
            var results = new List<RaycastResult>();
            var point = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, results);
            check(results.Count > 0 && results[0].gameObject.GetComponentInParent<Button>() == button,
                button.name + " 터치 경로 차단");
        }
        return $"Loading Layout {Screen.width}x{Screen.height}: {passed} checks passed (격리 Play Mode, raycast만 검사)";
    }

    private static bool IsOnScreen(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        foreach (var corner in corners)
        {
            var point = RectTransformUtility.WorldToScreenPoint(null, corner);
            if (point.x < -.5f || point.y < -.5f || point.x > Screen.width + .5f || point.y > Screen.height + .5f)
                return false;
        }
        return true;
    }

    public static string RunReferenceLayoutChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var root = new GameObject("ReferenceLayoutValidation", typeof(RectTransform), typeof(Canvas), typeof(CanvasSafeArea));
        root.hideFlags = HideFlags.HideAndDontSave;
        root.SetActive(false);
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(root.transform, false);
        var safe = root.GetComponent<CanvasSafeArea>();
        typeof(CanvasSafeArea).GetField("content", flags).SetValue(safe, content);
        typeof(CanvasSafeArea).GetField("fitReferenceResolution", flags).SetValue(safe, true);
        var layout = typeof(CanvasSafeArea).GetMethod("SetLayout", flags);
        int passed = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new InvalidOperationException(label); passed++; };
        try
        {
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            foreach (var size in new[] { new Vector2(1080, 1920), new Vector2(1080, 2400), new Vector2(3413.333f, 1920), new Vector2(1080, 607.5f), new Vector2(360, 640) })
            foreach (var area in new[] { new Rect(0, 0, 1, 1), new Rect(0, .05f, 1, .85f), new Rect(.06f, .04f, .88f, .9f) })
            {
                ((RectTransform)root.transform).sizeDelta = size;
                var reference = new Vector2(1080, 1920);
                layout.Invoke(safe, new object[] { area, size, reference });
                check(Vector2.Distance(content.rect.size, reference) < .01f, "참조 논리 크기 유지");
                var before = content.localScale;
                layout.Invoke(safe, new object[] { area, size, reference });
                check(Vector3.Distance(before, content.localScale) < .0001f, "반복 적용 배율 누적 방지");
                var corners = new Vector3[4]; content.GetWorldCorners(corners);
                var parent = (RectTransform)root.transform;
                var bounds = new Rect(parent.rect.min + Vector2.Scale(size, area.min), Vector2.Scale(size, area.size));
                bool inside = true;
                foreach (var corner in corners)
                {
                    var point = parent.InverseTransformPoint(corner);
                    inside &= point.x >= bounds.xMin - .01f && point.x <= bounds.xMax + .01f && point.y >= bounds.yMin - .01f && point.y <= bounds.yMax + .01f;
                }
                check(inside, "노치·제스처 안전 영역 안에 전체 내용 배치");
            }
            layout.Invoke(safe, new object[] { new Rect(0, 0, 1, 1), new Vector2(1080, 1920), Vector2.zero });
            check(content.localScale == Vector3.one && content.anchorMin == Vector2.zero && content.anchorMax == Vector2.one, "참조 크기 0에서 기본 배치 복원");
            typeof(CanvasSafeArea).GetMethod("OnDisable", flags).Invoke(safe, null);
            check(content.localScale == Vector3.one && content.anchorMin == Vector2.zero && content.anchorMax == Vector2.one, "비활성화 시 원래 배율·앵커 복원");
            return $"Reference Layout: {passed} checks passed (합성 해상도·inset, Edit Mode)";
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
