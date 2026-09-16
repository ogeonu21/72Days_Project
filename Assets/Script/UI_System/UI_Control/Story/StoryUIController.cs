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
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        NodeText = null;
        choiceListPresenter?.Clear();
    }

    public void UpdateUI(Node node)
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
        choiceListPresenter.Clear();

        yield return GameEvent.OnNodeTextUpdate(node.nodeMessage);

        yield return StartCoroutine(WaitForClick.WaitClick());

        //얘는 따로 MainStoryUIController나 그런거를 만들기가 힘드네.
        NodeManager.Instance.AdvanceMainStory(node);
    }

    public IEnumerator UpdateStoryNode(StoryNode node)
    {

        //dialogue Text 출력.
        yield return GameEvent.OnNodeTextUpdate(node.nodeMessage);
        choiceListPresenter.Present(node.choices, choice => NodeManager.Instance.SelectStoryChoice(choice));
    }
}
