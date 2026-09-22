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
    private Coroutine nodePresentation;

    protected virtual void OnEnable(){
        GameEvent.NodeTextUpdate += HandleNodeTextUpdate;
    }

    protected virtual void OnDisable(){
        GameEvent.NodeTextUpdate -= HandleNodeTextUpdate;
        CancelNodePresentation();
    }

    protected void StartNodePresentation(IEnumerator presentation)
    {
        CancelNodePresentation();
        nodePresentation = StartCoroutine(presentation);
    }

    private void CancelNodePresentation()
    {
        if (nodePresentation != null) StopCoroutine(nodePresentation);
        nodePresentation = null;
    }

    // 본문과 후속 선택지가 하나의 코루틴 수명을 공유하도록 한다.
    protected IEnumerator TypeNodeText(string text)
    {
        yield return TypewriterEffect.TypeTextCoroutine(NodeText, text ?? string.Empty, 0.05f);
    }

    IEnumerator HandleNodeTextUpdate(string text)
    {
        yield return TypeNodeText(text);
    }
}
