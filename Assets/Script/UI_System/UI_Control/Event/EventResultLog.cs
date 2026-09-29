using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>노드 UI와 독립적으로 결과를 한 줄씩 표시하고 사라지게 한다.</summary>
public sealed class EventResultLog : MonoBehaviour
{
    [SerializeField] private TMP_Text lineTemplate;
    [SerializeField] private float lineInterval = 0.2f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float lineSpacing = 44f;
    private readonly Queue<string> pending = new Queue<string>();
    private readonly List<(TMP_Text text, float born)> visible = new List<(TMP_Text, float)>();
    private float nextLineAt;

    public void Enqueue(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        pending.Enqueue(message.Trim());
    }

    private void Update() => Tick(Time.unscaledTime);

    public void Tick(float now)
    {
        for (int i = visible.Count - 1; i >= 0; i--)
        {
            var entry = visible[i];
            float age = now - entry.born;
            if (age >= lifetime)
            {
                Release(entry.text.gameObject);
                visible.RemoveAt(i);
            }
            else entry.text.alpha = 1f - Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime));
        }
        if (pending.Count == 0 || lineTemplate == null || now < nextLineAt) return;
        foreach (var entry in visible)
            entry.text.rectTransform.anchoredPosition += Vector2.up * lineSpacing;
        var text = Instantiate(lineTemplate, transform);
        text.name = "EventLogLine";
        text.text = pending.Dequeue();
        text.color = Color.white;
        text.fontSize = 36;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        text.gameObject.SetActive(true);
        visible.Add((text, now));
        nextLineAt = now + Mathf.Max(0.01f, lineInterval);
    }

    private void OnDisable()
    {
        pending.Clear();
        foreach (var entry in visible) if (entry.text != null) Release(entry.text.gameObject);
        visible.Clear();
        nextLineAt = 0;
    }

    private static void Release(GameObject value)
    {
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
