using UnityEngine;

[CreateAssetMenu(menuName = "Game/Defs/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{

    public string type;
    public string id;
    public string displayName;

    public BaseStats baseStats;  // str, dex, con
    public TuningStats tuningStats;
    public BaseItem dropItem;
    public float itemDropRate;
    public int dropGold;
}
