using System;
using System.Collections;
using UnityEngine;

public static class WaitForClick
{
    public static IEnumerator WaitClick()
    {
        return WaitForNewPress(HasContact, HasNewPress);
    }

    // 입력을 분리해 실제 기기 입력 없이도 코루틴의 경계 조건을 검증한다.
    internal static IEnumerator WaitForNewPress(Func<bool> hasContact, Func<bool> hasNewPress)
    {
        // 타이핑/선택에 쓰던 손가락을 모두 뗄 때까지 기다린다.
        while (hasContact()) yield return null;
        // 대기가 시작된 프레임의 클릭은 확인 입력으로 재사용하지 않는다.
        yield return null;
        while (!hasNewPress()) yield return null;
    }

    private static bool HasContact()
    {
        // Ended/Canceled도 해당 프레임에는 남아 있으므로 모두 사라진 뒤 대기한다.
        return Input.touchCount > 0 || (!Application.isMobilePlatform && Input.GetMouseButton(0));
    }

    private static bool HasNewPress()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).phase == TouchPhase.Began) return true;
            return false;
        }
        // 모바일의 터치→마우스 합성 입력을 중복 처리하지 않는다.
        return !Application.isMobilePlatform && Input.GetMouseButtonDown(0);
    }
}
