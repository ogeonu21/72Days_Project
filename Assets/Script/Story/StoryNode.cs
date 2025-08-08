using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "StoryNode", menuName = "Story/Node", order = 0)]
public class StoryNode : ScriptableObject
{
    [TextArea]
    public string dialogueText;

    public List<Choice> choices = new List<Choice>();

    //전투 트리거의 여부
    public bool triggersCombat;
    //전투 후 이어질 노드
    public StoryNode fallbackNode;

    //Editor 내부 위치
    public Vector2 editorPosition;

    public string combatEnemyID;
}

[System.Serializable]
public class Choice
{
    public string choiceText;

    //선택지와 이어질 노드
    public StoryNode nextNode;

    //상태 플래그
    public string requiredFlag;
    public string setFlag;

}
