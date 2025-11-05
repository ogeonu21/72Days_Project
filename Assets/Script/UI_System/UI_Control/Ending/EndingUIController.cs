using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class EndingUIController : UIController, IUpdatableUI
{
    public TMP_Text dialogueText;

    protected override void OnEnable()
    {
        base.OnEnable();
        NodeText = dialogueText;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        NodeText = null;
    }
    
    public void UpdateUI(Node node)
    {
        StartCoroutine(UpdateEndingUI(node as EndingNode));
    }

    public IEnumerator UpdateEndingUI(EndingNode node)
    {
        string message = node.endingName + "\n\n" +node.nodeMessage;

        yield return GameEvent.OnNodeTextUpdate(message);

        yield return StartCoroutine(WaitForClick.WaitClick());

        GameManager.Instance.BackToMain();
    }
}
