using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryUIController : UIController, IUpdatableUI
{
    public TMP_Text dialogueText;
    public Button[] choiceButtons;
    private ChoiceListPresenter choiceListPresenter;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (sharedView != null) dialogueText = sharedView.dialogueText;
        NodeText = dialogueText;
        choiceListPresenter = choiceListPresenter ?? new ChoiceListPresenter(choiceButtons);
        if (sharedView == null) choiceListPresenter.Clear();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        NodeText = null;
        if (sharedView == null) choiceListPresenter?.Clear();
    }

    public void UpdateUI(Node node)
    {
        if (node == null) return;
        if (node.nodeType == NodeType.StoryNode)
        {
            StartNodePresentation(UpdateStoryNode(node as StoryNode));
        }
        else if (node.nodeType == NodeType.MainStoryNode)
        {
            StartNodePresentation(UpdateMainStoryNode(node as MainStoryNode));
        }
    }

    public IEnumerator UpdateMainStoryNode(MainStoryNode node)
    {
        if (sharedView == null) choiceListPresenter.Clear();
        if (node == null) yield break;

        yield return TypeNodeText(node.nodeMessage);

        yield return WaitForClick.WaitClick();

        //얘는 따로 MainStoryUIController나 그런거를 만들기가 힘드네.
        NodeManager.Instance.AdvanceMainStory(node);
    }

    public IEnumerator UpdateStoryNode(StoryNode node)
    {

        if (sharedView == null) choiceListPresenter.Clear();
        if (node == null) yield break;
        yield return TypeNodeText(node.nodeMessage);
        if (sharedView == null)
            choiceListPresenter.Present(node.choices, choice => NodeManager.Instance.SelectStoryChoice(choice));
        else if (sharedView.OpenChoices(this))
        {
            sharedView.NewContent(this);
            foreach (var choice in node.choices)
            {
                if (choice == null || (string.IsNullOrWhiteSpace(choice.choiceText) && choice.nextNode == null)) continue;
                var selected = choice;
                sharedView.AddChoice(this, choice.choiceText, () => NodeManager.Instance.SelectStoryChoice(selected));
            }
        }
    }
}
