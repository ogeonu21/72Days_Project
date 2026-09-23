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
    private EventUIRouter eventRouter;
    
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
        if (eventRouter != null) eventRouter.Hide();
    }

    public void UpdateUI(Node node)
    {
        StartNodePresentation(UpdateEventNode(node as EventNode));

    }

    public IEnumerator UpdateEventNode(EventNode node)
    {
        choiceListPresenter.Clear();
        if (eventRouter != null) eventRouter.Hide();
        if (node == null) yield break;
        yield return TypeNodeText(node.nodeMessage);
        if (node.definition != null)
        {
            string error = EventDefinitionValidator.Validate(node.definition);
            if (error != null) { Debug.LogError(error); yield break; }
            if (eventRouter == null) eventRouter = GetComponent<EventUIRouter>() ?? gameObject.AddComponent<EventUIRouter>();
            eventRouter.Present(node, choiceButtons[0], dialogueText.font);
            yield break;
        }
        choiceListPresenter.Present(node.choices, choice => EventManager.Instance.Choose(choice));
    }
}
