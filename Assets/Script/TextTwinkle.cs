using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextTwinkle : MonoBehaviour
{
    [SerializeField]
    public string text;
    public TMP_Text targetText;
    private float delay = 0.0625f;

    public IEnumerator TextPrint(string inputText)
    {
        text = inputText;
        targetText.text = " ";
        int count = 0;

        while (count != text.Length)
        {
            if (count < text.Length)
            {
                targetText.text += text[count].ToString();
                count++;
            }

            yield return new WaitForSeconds(delay);
        }
        yield return new WaitForSeconds(0.5f);
    }
    public IEnumerator TextPrintln(string inputText)
    {
        text = inputText;
        targetText.text += "\n";
        int count = 0;
        while (count != text.Length)
        {
            if (count < text.Length)
            {
                targetText.text += text[count].ToString();
                count++;
            }

            yield return new WaitForSeconds(delay);
        }
        yield return new WaitForSeconds(0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
