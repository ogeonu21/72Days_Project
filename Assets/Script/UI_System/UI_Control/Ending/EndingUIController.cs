using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class EndingUIController : UIController, IUpdatableUI
{
    public TMP_Text dialogueText;
    
    public void UpdateUI(Node node)
    {
        StartCoroutine(UpdateEndingUI(node as EndingNode));


    }

    public IEnumerator UpdateEndingUI(EndingNode node)
    {
        string message = node.endingName + "\n\n" +node.nodeMessage;

        yield return this.StartCoroutine(TypewriterEffect.TypeTextCoroutine(dialogueText, message, 0.05f));

        yield return StartCoroutine(WaitForClick.WaitClick());

        GameManager.Instance.BackToMain();
    }
}
