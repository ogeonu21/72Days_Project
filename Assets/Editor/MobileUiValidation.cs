using System;
using UnityEngine;
using UnityEditor;

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
}
