using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "StoryNode", menuName = "Story/Node", order = 0)]
public class StoryNode : ScriptableObject
{
    [TextArea]
    public string dialogueText;

    public List<Choice> choices = new List<Choice>();

    //Editor 내부 위치
    public Vector2 editorPosition;
}

[System.Serializable]
public class Choice
{
    public string choiceText;

    //선택지와 이어질 노드
    public StoryNode nextNode;

    //상태 플래그
    [Header("상태 플래그 / 추후 추가 예정")]
    public string requiredFlag;
    public string setFlag;

    //전투 트리거의 여부
    [Header("전투 여부 / 전투 대상 ID")]
    public bool triggersCombat;
    public string combatEnemyID;
}
