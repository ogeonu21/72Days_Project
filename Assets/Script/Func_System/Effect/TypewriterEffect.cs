using System.Collections;
using UnityEngine;
using TMPro;

public static class TypewriterEffect
{
    private static bool isTyping;

    public static void StartTyping(MonoBehaviour monoBehaviour, TMP_Text targetTextComponent, string textToType, float typingSpeed = 0.05f)
    {
        monoBehaviour.StartCoroutine(TypeTextCoroutine(targetTextComponent, textToType, typingSpeed));
    }

    public static IEnumerator TypeTextCoroutine(TMP_Text targetTextComponent, string textToType, float typingSpeed)
    {
        if (targetTextComponent == null)
        {
            Debug.LogError("타이핑 효과를 적용할 텍스트 컴포넌트가 null입니다.");
            yield break;
        }

        targetTextComponent.text = "";

        foreach (char letter in textToType.ToCharArray())
        {
            targetTextComponent.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}
