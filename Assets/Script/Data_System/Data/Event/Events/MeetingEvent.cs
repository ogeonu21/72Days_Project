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
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.ChangeGoodAndEvil(1);
                        }
                    }
                    Debug.Log($"<color=cyan>[Event] </color>{who.enemyData.name}과(와) 전투를 시작합니다.");
                    who.successNode = nextNode;
                    NodeManager.Instance.GoToNode(who);
                    break;
                case meetingEventType.무시하기:
                    Debug.Log($"<color=cyan>[Event] </color>{who.enemyData.name}을(를) 무시합니다.");
                    NodeManager.Instance.GoToNode(nextNode);
                    break;
                case meetingEventType.도와주기:
                    //조건 사용.
                    //보상 제공
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ChangeGoodAndEvil(-1);
                    }
                    Debug.Log($"<color=cyan>[Event] </color>{who.enemyData.name}을(를) 도와줍니다.");
                    NodeManager.Instance.GoToNode(nextNode);
                    break;
                default:
                    Debug.LogWarning($"<color=cyan>[Event] </color>알 수 없는 EventType입니다.");
                    break;
            }
        }
        
    }
}
