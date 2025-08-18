using UnityEngine;

[CreateAssetMenu(menuName = "Game/Defs/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    public string id;
    public string displayName;

    public BaseStats baseStats;  // str, dex, con
    public int attackBonus;
    public int hpBonus;
    [Range(0f, 1f)] public float dodgeBonus;
    public int rangeBonus;
}
