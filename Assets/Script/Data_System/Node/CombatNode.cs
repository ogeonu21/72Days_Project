using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
[CreateAssetMenu(fileName = "CombatNode", menuName = "Node/CombatNode", order = 2)]
public class CombatNode : Node
{
    public string combatEnemyID;
    public Node successNode;
    public Node failureNode;
    public EnemyDefinition enemyData;
}
