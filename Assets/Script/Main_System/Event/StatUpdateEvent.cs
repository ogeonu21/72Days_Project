using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatUpdateEvent", menuName = "Events/StatUpdateEvent")]
public class StatUpdateEvent : BaseEvent
{
    public string eventCategory;
    public int changeAmount;
    public string statType;

    public override void Execute()
    {
        EventExecute();
    }

    public override void Execute(Node nextNode)
    {
        EventExecute();

        if (nextNode != null)
        {
            NodeManager.Instance.GoToNode(nextNode);
        }
        
    }

    public void EventExecute()
    {
        if (CharacterManager.Instance != null && NodeManager.Instance != null)
        {
            switch (statType)
            {
                case "str":
                    CharacterManager.Instance.currentPlayer.baseStats.str += changeAmount;
                    break;
                case "dex":
                    CharacterManager.Instance.currentPlayer.baseStats.dex += changeAmount;
                    break;
                case "con":
                    CharacterManager.Instance.currentPlayer.baseStats.con += changeAmount;
                    break;
                default:
                    Debug.Log($"StatUpdateEvent: statType Error");
                    break;
            }
            CharacterManager.Instance.currentPlayer.UpdateStats();
        }
    }
}