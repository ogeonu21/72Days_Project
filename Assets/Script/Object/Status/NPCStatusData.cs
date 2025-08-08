using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "GameData/NPCStatusData")]
public class NPCStatusData : ScriptableObject
{
    public string npcName;
    public Status baseStatus;

    [SerializeField]
    public string description;
}
