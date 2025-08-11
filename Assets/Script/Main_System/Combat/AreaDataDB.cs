using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct AreaData
{
    public string label;
    public float hitRate; // 0~1
    public float effectRate; // 0~1
    public float damageMultiplier; // >= 0

    public AreaData(string label, float hitRate, float effectRate, float damageMultiplier)
    {
        this.label = label.Trim();
        this.hitRate = Mathf.Clamp01(hitRate);
        this.effectRate = Mathf.Clamp01(effectRate);
        this.damageMultiplier = Mathf.Max(0f, damageMultiplier);
    }
}


public static class AreaDataDB
{
    public static readonly AreaData[] All =
    {
        new AreaData("¸Ó¸®", 0.4f, 0.6f, 1.6f),
        new AreaData("¸ö", 0.9f, 0.15f, 0.7f),
        new AreaData("ÆÈ", 0.65f, 0.3f, 1.0f),
        new AreaData("´Ù¸®", 0.7f, 0.2f, 1.1f)
    };

    private static readonly Dictionary<string, AreaData> ByLabel = All.ToDictionary(a => a.label, a => a);

    public static AreaData GetArea(string label, out AreaData data)
    {
        ByLabel.TryGetValue(label, out data);
        return data;
    }

}
