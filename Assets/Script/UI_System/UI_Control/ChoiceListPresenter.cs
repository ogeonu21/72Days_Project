using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 선택 버튼의 표시와 리스너 생명주기를 한 곳에서 관리한다.
/// 노드 진행 규칙은 소유하지 않고 선택 결과만 호출자에게 전달한다.
/// </summary>
public class ChoiceListPresenter
{
    private readonly Button[] buttons;
    private readonly TMP_Text[] labels;

    public ChoiceListPresenter(Button[] buttons)
    {
        this.buttons = buttons ?? Array.Empty<Button>();
        labels = new TMP_Text[this.buttons.Length];

        for (int index = 0; index < this.buttons.Length; index++)
        {
            labels[index] = this.buttons[index] != null ? this.buttons[index].GetComponentInChildren<TMP_Text>() : null;
        }
    }

    public void Clear()
    {
        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();
            button.gameObject.SetActive(false);
        }
    }

    public void Present(IList<Choice> choices, Action<Choice> onSelected)
    {
        Clear();
        if (choices == null || onSelected == null)
        {
            return;
        }

        int buttonIndex = 0;
        foreach (Choice choice in choices)
        {
            if (buttonIndex >= buttons.Length)
            {
                Debug.LogWarning("[ChoiceListPresenter] 표시 가능한 선택 버튼 수를 초과했습니다.");
                return;
            }

            if (choice == null || (string.IsNullOrWhiteSpace(choice.choiceText) && choice.nextNode == null))
            {
                continue;
            }

            Button button = buttons[buttonIndex];
            TMP_Text label = labels[buttonIndex];
            buttonIndex++;

            if (button == null || label == null)
            {
                Debug.LogError("[ChoiceListPresenter] 버튼 또는 TextMeshPro 라벨 참조가 없습니다.");
                continue;
            }

            Choice selectedChoice = choice;
            label.text = selectedChoice.choiceText;
            button.gameObject.SetActive(true);
            button.onClick.AddListener(() => onSelected(selectedChoice));
        }
    }
}
