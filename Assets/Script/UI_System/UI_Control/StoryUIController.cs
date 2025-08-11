using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryUIController : MonoBehaviour
{
    public TMP_Text dialogueText;
    public Button[] choiceButtons; // 버튼 3개를 배열로 미리 연결
    private StoryManager storyManager;

    private void Awake()
    {
        storyManager = StoryManager.Instance;
        storyManager.OnStoryNodeChanged += UpdateStoryUIWrapper;
    }

    public void UpdateStoryUIWrapper(StoryNode node)
    {
            StartCoroutine(UpdateStoryUI(node));
    }

    public IEnumerator UpdateStoryUI(StoryNode node)
    {
        // 모든 버튼 비활성화
        foreach (var btn in choiceButtons)
        {
            btn.gameObject.SetActive(false);
            btn.onClick.RemoveAllListeners(); // 기존 리스너 제거
        }

        //dialogue Text 출력.
        yield return this.StartCoroutine(TypewriterEffect.TypeTextCoroutine(dialogueText, node.dialogueText, 0.05f));


        if (node.choices != null && node.choices.Count > 0)
        {
            for (int i = 0; i < node.choices.Count && i < choiceButtons.Length; i++)    
            {
                if (node.choices[i].choiceText != "" || node.choices[i].nextNode != null) {
                    int index = i;
                    var choice = node.choices[i];

                    choiceButtons[i].gameObject.SetActive(true);
                    choiceButtons[i].GetComponentInChildren<TMP_Text>().text = choice.choiceText;
                    choiceButtons[i].onClick.AddListener(() => storyManager.Choose(index));
                }
            }
        }
        yield break;
    }
}
