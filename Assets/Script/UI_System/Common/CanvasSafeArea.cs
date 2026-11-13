using UnityEngine;

/// <summary>화면 카메라의 레터박스와 기기 안전 영역의 교집합 안에 UI를 배치한다.</summary>
[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public sealed class CanvasSafeArea : MonoBehaviour
{
    [SerializeField] private bool fitReferenceResolution;
    private Canvas canvas;
    private UnityEngine.UI.CanvasScaler scaler;
    private RectTransform content;
    private Rect lastSafeArea;
    private Rect lastViewport;
    private Vector2 lastCanvasSize, lastReference;
    private bool lastFit;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
        if (canvas.renderMode == RenderMode.WorldSpace || !canvas.isRootCanvas)
        {
            Debug.LogError("[CanvasSafeArea] 루트 화면 Canvas에만 적용할 수 있습니다.", this);
            enabled = false;
            return;
        }
        int childCount = transform.childCount;
        content = new GameObject("SafeAreaContent", typeof(RectTransform)).GetComponent<RectTransform>();
        content.gameObject.layer = gameObject.layer;
        content.SetParent(transform, false);
        SetAnchors(new Rect(0, 0, 1, 1));
        // 부모 크기가 동일한 상태에서 기존 앵커와 오프셋을 보존한다.
        for (int i = 0; i < childCount; i++)
        {
            var child = transform.GetChild(0);
            var rect = child as RectTransform;
            Vector3 position = rect != null ? rect.anchoredPosition3D : child.localPosition;
            Vector2 size = rect != null ? rect.sizeDelta : Vector2.zero;
            child.SetParent(content, false);
            if (rect != null)
            {
                rect.anchoredPosition3D = position;
                rect.sizeDelta = size;
            }
        }
        Apply(true);
    }

    private void OnEnable() { if (content != null) Apply(true); }
    private void LateUpdate() { Apply(false); }
    private void OnDisable() { if (content != null) SetAnchors(new Rect(0, 0, 1, 1)); }

    private void Apply(bool force)
    {
        if (content == null) return;
        Rect viewport = canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null
            ? canvas.worldCamera.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
        Rect safeArea = Screen.safeArea;
        Vector2 canvasSize = ((RectTransform)transform).rect.size;
        Vector2 reference = scaler != null ? scaler.referenceResolution : Vector2.zero;
        if (!force && safeArea == lastSafeArea && viewport == lastViewport && canvasSize == lastCanvasSize &&
            reference == lastReference && fitReferenceResolution == lastFit) return;
        lastSafeArea = safeArea;
        lastViewport = viewport;
        lastCanvasSize = canvasSize;
        lastReference = reference;
        lastFit = fitReferenceResolution;
        SetLayout(NormalizeSafeArea(safeArea, viewport), canvasSize, reference);
    }

    private void SetLayout(Rect area, Vector2 canvasSize, Vector2 reference)
    {
        if (fitReferenceResolution && canvasSize.x > 0 && canvasSize.y > 0 && reference.x > 0 && reference.y > 0)
        {
            // 자식의 고정 오프셋과 동적 패널 좌표를 보존한 채 전체를 안전 영역 안에 맞춘다.
            content.anchorMin = content.anchorMax = area.center;
            content.pivot = new Vector2(.5f, .5f);
            content.sizeDelta = reference;
            content.anchoredPosition = Vector2.zero;
            float scale = Mathf.Min(canvasSize.x * area.width / reference.x, canvasSize.y * area.height / reference.y);
            content.localScale = new Vector3(scale, scale, 1);
        }
        else SetAnchors(area);
    }

    public static Rect NormalizeSafeArea(Rect safeArea, Rect viewport)
    {
        if (viewport.width <= 0 || viewport.height <= 0) return new Rect(0, 0, 1, 1);
        float minX = Mathf.Clamp01((safeArea.xMin - viewport.xMin) / viewport.width);
        float minY = Mathf.Clamp01((safeArea.yMin - viewport.yMin) / viewport.height);
        float maxX = Mathf.Clamp01((safeArea.xMax - viewport.xMin) / viewport.width);
        float maxY = Mathf.Clamp01((safeArea.yMax - viewport.yMin) / viewport.height);
        if (maxX <= minX || maxY <= minY) return new Rect(0, 0, 1, 1);
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private void SetAnchors(Rect area)
    {
        content.localScale = Vector3.one;
        content.anchorMin = area.min;
        content.anchorMax = area.max;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
    }
}
