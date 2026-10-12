using System;
using System.Collections;
using UnityEngine;

public static class WaitForClick
{
    public static IEnumerator WaitClick()
    {
        return WaitForNewPress(HasNewPress);
    }

    // 입력을 분리해 실제 기기 입력 없이도 코루틴의 경계 조건을 검증한다.
    internal static IEnumerator WaitForNewPress(Func<bool> hasNewPress)
    {
        // 대기가 시작된 프레임의 클릭은 확인 입력으로 재사용하지 않는다.
        yield return null;
        // 새 Down/Began만 받으므로 길게 누르기는 통과하지 않는다.
        // 모든 접촉의 해제를 요구하면 연속 탭/다중 터치의 새 입력까지 버리게 된다.
        while (!hasNewPress()) yield return null;
    }

    private static bool HasNewPress()
    {
        bool touchBegan = false;
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
                if (Input.GetTouch(i).phase == TouchPhase.Began) { touchBegan = true; break; }
        }
        return AcceptNewPress(Application.isEditor, Application.isMobilePlatform,
            Input.GetMouseButtonDown(0), touchBegan);
    }

    internal static bool AcceptNewPress(bool isEditor, bool isMobile, bool mouseDown, bool touchBegan)
    {
        // Game 창의 마우스는 허용하되 모바일 빌드의 합성 마우스는 제외한다.
        return touchBegan || ((isEditor || !isMobile) && mouseDown);
    }
}
