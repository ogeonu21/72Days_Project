using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatUpdateEvent", menuName = "Events/StatUpdateEvent")]
public class StatUpdateEvent : BaseEvent
{
    public int changeAmount;
    public string statType;

    public override void Execute()
    {
        EventExecute();
    }

    public override void Execute(Node nextNode)
    {
        NodeManager.Instance.dumpNode.nextNode = nextNode;
        NodeManager.Instance.dumpNode.surviveDate = nextNode.surviveDate;
        NodeManager.Instance.dumpNode.worldLocation = nextNode.worldLocation;
        EventExecute();
        
    }

    public void EventExecute()
    {
        if (CharacterManager.Instance != null && NodeManager.Instance != null)
        {
            CharacterManager.Instance.currentPlayer.UpdateBaseStats(statType, changeAmount);
            
            NodeManager.Instance.dumpNode.nodeMessage = $"{StatTypeTranslation(statType)}이 {changeAmount}만큼 증가했다!";

            NodeManager.Instance.GoToNode(NodeManager.Instance.dumpNode);
        }
    }
    private string StatTypeTranslation(string statType)
    {
        switch (statType)
            {
                case "str" :
                return "힘";
                case "con" :
                return "체력";
                case "dex" :
                return "민첩";
                default :
                return "에러";
            }
    }
}