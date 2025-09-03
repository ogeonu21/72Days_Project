using UnityEngine;

[CreateAssetMenu(fileName = "MeetingEvent", menuName = "Events/MeetingEvent")]
public class MeetingEvent : BaseEvent
{
    public enum meetingEventType
    {
        전투하기,
        무시하기,
        도와주기
    }

    public string eventCategory;
    public meetingEventType type;
    public CombatNode who;

    public override void Execute(Node nextNode)
    {
        if (NodeManager.Instance != null) {
            switch (type)
            {
                case meetingEventType.전투하기:
                    if (who.enemyData.type == "선")
                    {
                        //선량한 사람과 싸워? 너새끼는 쓰레기다.
                        Debug.Log("쓰레기새끼.");
                    }
                    who.successNode = nextNode;
                    NodeManager.Instance.GoToNode(who);
                    break;
                case meetingEventType.무시하기:
                    NodeManager.Instance.GoToNode(nextNode);
                    break;
                case meetingEventType.도와주기:
                    //조건 사용.
                    //보상 제공
                    NodeManager.Instance.GoToNode(nextNode);
                    break;
                default:
                    Debug.LogWarning("에러에러에러에러에러에러!!!!!!!!!!!!!! meetingEventType 오류!!!!");
                    break;
            }
        }
        
    }
}
