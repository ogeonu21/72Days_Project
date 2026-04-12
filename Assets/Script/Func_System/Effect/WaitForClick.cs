using System;
using System.Collections;
using UnityEngine;

public static class WaitForClick
{
    public static IEnumerator WaitClick()
    {
        while (Input.touchCount == 0 && !Input.GetMouseButtonDown(0))
        {
            yield return null; // 한 프레임 대기
        }
    }
}
