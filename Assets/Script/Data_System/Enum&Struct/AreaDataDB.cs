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
        this.hitRate = hitRate;
        this.effectRate = effectRate;
        this.damageMultiplier = Mathf.Max(0f, damageMultiplier);
    }
}

