using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{
    private void Awake()
    {
        // Assets/Resources/NPCStats/<ID>.asset 형태로 저장할 예정
        var def = Resources.Load<EnemyDefinition>($"NPCStats/{ID}");
        if (def != null)
        {
            InitializeFromDefinition(def);
        }
        else
        {
            Debug.LogWarning($"EnemyDefinition not found for ID='{ID}' (Resources/NPCStats/{ID})", this);
            UpdateStats();
            SetCurrentHPToMaxAndNotify();
        }
    }
}
