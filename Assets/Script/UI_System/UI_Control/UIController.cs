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

    protected virtual void OnEnable(){
        GameEvent.NodeTextUpdate += HandleNodeTextUpdate;
    }

    protected virtual void OnDisable(){
        GameEvent.NodeTextUpdate -= HandleNodeTextUpdate;
    }

    IEnumerator HandleNodeTextUpdate(string text)
    {
        yield return this.StartCoroutine(TypewriterEffect.TypeTextCoroutine(NodeText, text, 0.05f));
    }
}
