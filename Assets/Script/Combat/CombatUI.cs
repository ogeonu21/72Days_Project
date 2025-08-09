using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUI : MonoBehaviour
{
    public TextMeshProUGUI message;
    public GameObject buttonPrefab;
    public Transform buttonContainer;

    public IEnumerator ShowMessage(string text)
    {
        message.text = text;
        yield return new WaitForSeconds(1.5f);
    }

    public void ShowChoices(string[] options, Action<string> onSelect)
    {
        foreach (Transform c in buttonContainer) Destroy(c.gameObject);

        foreach (var o in options)
        {
            var btn = Instantiate(buttonPrefab, buttonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = o;
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                onSelect(o);
                foreach (Transform c in buttonContainer) Destroy(c.gameObject);
            });
        }
    }
}