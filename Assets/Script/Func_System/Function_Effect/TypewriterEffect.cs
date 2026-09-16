using System.Collections;
using UnityEngine;
using TMPro;

public static class TypewriterEffect
{
    // 코루틴 자체를 반환하여 호출하는 쪽에서 제어하도록 함
    public static IEnumerator TypeTextCoroutine(TMP_Text targetTextComponent, string textToType, float typingSpeed = 0.05f)
    {
        if (targetTextComponent == null)
        {
            Debug.LogError("타이핑 효과를 적용할 텍스트 컴포넌트가 null입니다.");
            yield break;
        }

        targetTextComponent.text = "";

        // 이 루프가 끝날 때까지 타이핑이 진행됨
        foreach (char letter in textToType.ToCharArray())
        {
            targetTextComponent.text += letter;
            yield return new WaitForSeconds(typingSpeed-0.03f);
        }
    }
}