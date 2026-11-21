using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public interface IUpdatableUI
{
    void UpdateUI(Node node);
}

public class UIController : MonoBehaviour
{
    public TMP_Text NodeText;
    public SharedNodeView sharedView;
    private Coroutine nodePresentation;

    protected virtual void OnEnable(){
        if (sharedView != null) sharedView.Enter(this);
        else GameEvent.NodeTextUpdate += HandleNodeTextUpdate;
    }

    protected virtual void OnDisable(){
        GameEvent.NodeTextUpdate -= HandleNodeTextUpdate;
        CancelNodePresentation();
        if (sharedView != null) sharedView.Exit(this);
    }

    protected void StartNodePresentation(IEnumerator presentation)
    {
        CancelNodePresentation();
        if (sharedView != null) sharedView.Enter(this);
        nodePresentation = StartCoroutine(presentation);
    }

    private void CancelNodePresentation()
    {
        if (nodePresentation != null) StopCoroutine(nodePresentation);
        nodePresentation = null;
    }

    // 본문과 후속 선택지가 하나의 코루틴 수명을 공유하도록 한다.
    protected IEnumerator TypeNodeText(string text) => PresentNodeText(text);

    public IEnumerator PresentNodeText(string text, float speed = .05f)
    {
        if (sharedView != null) yield return sharedView.PresentText(this, text, speed);
        else yield return TypewriterEffect.TypeTextCoroutine(NodeText, text ?? string.Empty, speed);
    }

    IEnumerator HandleNodeTextUpdate(string text)
    {
        yield return TypeNodeText(text);
    }
}
