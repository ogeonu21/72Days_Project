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
        NodeText = dialogueText;
        choiceListPresenter = choiceListPresenter ?? new ChoiceListPresenter(choiceButtons);
        choiceListPresenter.Clear();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        NodeText = null;
        choiceListPresenter?.Clear();
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
        choiceListPresenter.Clear();
        if (node == null) yield break;

        yield return TypeNodeText(node.nodeMessage);

        yield return WaitForClick.WaitClick();

        //얘는 따로 MainStoryUIController나 그런거를 만들기가 힘드네.
        NodeManager.Instance.AdvanceMainStory(node);
    }

    public IEnumerator UpdateStoryNode(StoryNode node)
    {

        choiceListPresenter.Clear();
        if (node == null) yield break;
        yield return TypeNodeText(node.nodeMessage);
        choiceListPresenter.Present(node.choices, choice => NodeManager.Instance.SelectStoryChoice(choice));
    }
}
