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
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        NodeText = null;
        choiceListPresenter?.Clear();
    }

    public void UpdateUI(Node node)
    {
        StartCoroutine(UpdateEventNode(node as EventNode));

    }

    public IEnumerator UpdateEventNode(EventNode node)
    {
        yield return GameEvent.OnNodeTextUpdate(node.nodeMessage);
        choiceListPresenter.Present(node.choices, choice => EventManager.Instance.Choose(choice));
    }
}
