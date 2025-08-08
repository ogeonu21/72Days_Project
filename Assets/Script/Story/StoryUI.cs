using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryUIController : MonoBehaviour
{
    public TMP_Text dialogueText;
    public Button[] choiceButtons; // 버튼 3개를 배열로 미리 연결
    private StoryManager storyManager;

    private void Start()
    {
        storyManager = StoryManager.Instance;
        storyManager.OnNodeChanged += DisplayNode;
    }

    public void DisplayNode(StoryNode node)
    {
        dialogueText.text = node.dialogueText;

        // 모든 버튼 비활성화
        foreach (var btn in choiceButtons)
        {
            btn.gameObject.SetActive(false);
            btn.onClick.RemoveAllListeners(); // 기존 리스너 제거
        }

        if (node.choices != null && node.choices.Count > 0)
        {
            for (int i = 0; i < node.choices.Count && i < choiceButtons.Length; i++)
            {
                int index = i;
                var choice = node.choices[i];

                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TMP_Text>().text = choice.choiceText;
                choiceButtons[i].onClick.AddListener(() => storyManager.Choose(index));
            }
        }
        else if (node.fallbackNode != null)
        {
            storyManager.GoToNode(node.fallbackNode);
        }
    }
}
