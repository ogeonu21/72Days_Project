using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/PlayerStatusData")]
public class PlayerStatusData : ScriptableObject
{
    public Status baseStatus;
    //데이터에서 플레이어의 기본 데이터를 뽑아오기 위한 기본 이름.
    public string playerBaseName = "Player";
    //플레이어가 새롭게 지정할 수 있는 플레이어의 이름.
    public string playerNewName;
}
