using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventUIController : UIController, IUpdatableUI
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
        StartNodePresentation(UpdateEventNode(node as EventNode));

    }

    public IEnumerator UpdateEventNode(EventNode node)
    {
        choiceListPresenter.Clear();
        if (node == null) yield break;
        yield return TypeNodeText(node.nodeMessage);
        choiceListPresenter.Present(node.choices, choice => EventManager.Instance.Choose(choice));
    }
}
