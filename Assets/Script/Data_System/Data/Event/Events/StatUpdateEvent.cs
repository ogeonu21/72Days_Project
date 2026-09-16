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
            switch (statType)
            {
                case "str":
                    CharacterManager.Instance.currentPlayer.baseStats.str += changeAmount;
                    NodeManager.Instance.dumpNode.nodeMessage = $"힘이 {changeAmount}만큼 증가했다!";
                    break;
                case "dex":
                    CharacterManager.Instance.currentPlayer.baseStats.dex += changeAmount;
                    NodeManager.Instance.dumpNode.nodeMessage = $"민첩성이 {changeAmount}만큼 증가했다!";
                    break;
                case "con":
                    CharacterManager.Instance.currentPlayer.baseStats.con += changeAmount;
                    NodeManager.Instance.dumpNode.nodeMessage = $"체력이 {changeAmount}만큼 증가했다!";
         
                    break;
                default:
                    Debug.Log($"StatUpdateEvent: statType Error");
                    break;
            }
            CharacterManager.Instance.currentPlayer.UpdateStats();
            NodeManager.Instance.GoToNode(NodeManager.Instance.dumpNode);
        }
    }
}