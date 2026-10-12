using UnityEngine;
using UnityEngine.UI;

/// <summary>게임 체력과 분리된 HP바 표시. 감소만 루트 곡선으로 보간한다.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Slider))]
public sealed class HealthBarAnimation : MonoBehaviour
{
    [SerializeField, Min(.01f)] private float decreaseDuration = .6f;
    private Slider slider;
    private bool initialized;
    private float startValue;
    private float targetValue;
    private float elapsed;
    public bool IsAnimating { get; private set; }

    public void SetValue(float normalizedValue, bool animate = true)
    {
        if (slider == null) slider = GetComponent<Slider>();
        float value = Mathf.Clamp01(normalizedValue);
        if (!initialized || !animate || !isActiveAndEnabled || value > targetValue)
        {
            initialized = true;
            targetValue = value;
            SnapToTarget();
            return;
        }
        // 중복 스탯 알림으로 애니메이션 시간을 되돌리지 않는다.
        if (Mathf.Approximately(value, targetValue)) return;
        startValue = slider.normalizedValue;
        targetValue = value;
        elapsed = 0;
        IsAnimating = startValue > targetValue;
        if (!IsAnimating) SnapToTarget();
    }

    private void Update() => Advance(Time.unscaledDeltaTime);

    public void Advance(float deltaTime)
    {
        if (!IsAnimating || slider == null) return;
        elapsed += Mathf.Max(0, deltaTime);
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, decreaseDuration));
        slider.normalizedValue = Evaluate(startValue, targetValue, progress);
        if (progress >= 1) SnapToTarget();
    }

    public static float Evaluate(float from, float to, float progress)
    {
        return Mathf.Lerp(from, to, Mathf.Sqrt(Mathf.Clamp01(progress)));
    }

    private void OnDisable()
    {
        if (initialized) SnapToTarget();
    }

    private void SnapToTarget()
    {
        IsAnimating = false;
        elapsed = 0;
        if (slider != null) slider.normalizedValue = targetValue;
    }
}
