using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventUIController : UIController, IUpdatableUI
{
    public TMP_Text dialogueText;
    public Button[] choiceButtons;
    

    public void UpdateUI(Node node)
    {
        StartCoroutine(UpdateEventNode(node as EventNode));

        EventManager.Instance.EventNodeStart(node as EventNode);
    }

    public IEnumerator UpdateEventNode(EventNode node)
    {
        foreach (var btn in choiceButtons)
        {
            btn.gameObject.SetActive(false);
            btn.onClick.RemoveAllListeners();
        }

        yield return this.StartCoroutine(TypewriterEffect.TypeTextCoroutine(dialogueText, node.nodeMessage, 0.05f));

        if (node.choices != null && node.choices.Count > 0)
        {
            for (int i = 0; i < node.choices.Count && i < choiceButtons.Length; i++)
            {
                if (node.choices[i].choiceText != "" || node.choices[i].nextNode != null)
                {
                    int index = i;
                    var choice = node.choices[i];

                    choiceButtons[i].gameObject.SetActive(true);
                    choiceButtons[i].GetComponentInChildren<TMP_Text>().text = choice.choiceText;
                    choiceButtons[i].onClick.AddListener(() => EventManager.Instance.Choose(choice));
                }
            }
        }
    }
}
