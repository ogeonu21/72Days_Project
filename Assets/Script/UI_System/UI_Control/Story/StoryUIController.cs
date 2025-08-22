using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryUIController : MonoBehaviour
{
    public TMP_Text dialogueText;
    public Button[] choiceButtons;

    private void Awake()
    {
    }

    private void OnEable()
    {
        UpdateStoryUI(NodeManager.Instance.currentNode);
    }

    public void UpdateStoryUI(Node node)
    {
        if (node.nodeType == NodeType.StoryNode)
        {
            StartCoroutine(UpdateStoryNode(node as StoryNode));
        }
        else if (node.nodeType == NodeType.MainStoryNode)
        {
            StartCoroutine(UpdateMainStoryNode(node as MainStoryNode));
        }
    }

    public IEnumerator UpdateMainStoryNode(MainStoryNode node)
    {
        foreach (var btn in choiceButtons)
        {
            btn.gameObject.SetActive(false);
            btn.onClick.RemoveAllListeners(); // 기존 리스너 제거
        }

        yield return this.StartCoroutine(TypewriterEffect.TypeTextCoroutine(dialogueText, node.nodeMessage, 0.05f));

        yield return StartCoroutine(WaitForClick.WaitClick());

        //얘는 따로 MainStoryUIController나 그런거를 만들기가 힘드네.
        NodeManager.Instance.GoToNode(node.nextNode);
    }

    public IEnumerator UpdateStoryNode(StoryNode node)
    {

        foreach (var btn in choiceButtons)
        {
            btn.gameObject.SetActive(false);
            btn.onClick.RemoveAllListeners(); // 기존 리스너 제거
        }

        //dialogue Text 출력.
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
                    choiceButtons[i].onClick.AddListener(() => NodeManager.Instance.GoToNode(choice.nextNode));
                }
            }
        }
        yield break;
    }
}
    