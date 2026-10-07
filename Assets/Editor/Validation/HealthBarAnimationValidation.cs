using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HealthBarAnimationValidation
{
    [MenuItem("Tools/Validation/Health Bar Animation")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        int count = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); count++; };
        var root = new GameObject("HealthBarFixture", typeof(RectTransform), typeof(Slider), typeof(HealthBarAnimation))
            { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var slider = root.GetComponent<Slider>();
            var view = root.GetComponent<HealthBarAnimation>();
            view.SetValue(1);
            check(slider.normalizedValue == 1 && !view.IsAnimating, "최초 표시 즉시 동기화");
            view.SetValue(.2f);
            check(slider.normalizedValue == 1 && view.IsAnimating, "피해 시 즉시 점프 없음");
            view.Advance(.15f);
            check(Mathf.Abs(slider.normalizedValue - .6f) < .0001f, "시간 25%에 감소량 50% 반영");
            view.SetValue(.2f);
            view.Advance(.15f);
            check(Mathf.Abs(slider.normalizedValue - HealthBarAnimation.Evaluate(1,.2f,.5f)) < .0001f, "중복 알림은 진행 유지");
            float current = slider.normalizedValue;
            view.SetValue(0);
            check(slider.normalizedValue == current, "연속 피해는 현재 위치에서 시작");
            view.Advance(.15f);
            check(Mathf.Abs(slider.normalizedValue - current * .5f) < .0001f, "새 피해 목표로 루트 보간");
            view.Advance(1);
            check(slider.normalizedValue == 0 && !view.IsAnimating, "사망 체력 정확히 0");
            view.SetValue(.8f);
            check(slider.normalizedValue == .8f && !view.IsAnimating, "회복 즉시 표시");
            view.SetValue(.2f); view.Advance(.1f); view.SetValue(.5f);
            check(slider.normalizedValue == .5f && !view.IsAnimating, "감소 도중 회복은 이전 보간 취소");
            view.SetValue(.1f, false);
            check(slider.normalizedValue == .1f && !view.IsAnimating, "초기화/에디터 표시 즉시 반영");
            view.SetValue(1); view.SetValue(.4f);
            typeof(HealthBarAnimation).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(view,null);
            check(slider.normalizedValue == .4f && !view.IsAnimating, "숨김 시 오래된 애니메이션 정리");
            check(HealthBarAnimation.Evaluate(1,0,.25f) - HealthBarAnimation.Evaluate(1,0,.5f) >
                HealthBarAnimation.Evaluate(1,0,.75f) - HealthBarAnimation.Evaluate(1,0,1), "후반 감소 속도 완화");
            check(HealthBarAnimation.Evaluate(1,0,-1) == 1 && HealthBarAnimation.Evaluate(1,0,2) == 0, "보간 범위 제한");
            return $"Health bar animation: {count} checks passed (Edit Mode)";
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
