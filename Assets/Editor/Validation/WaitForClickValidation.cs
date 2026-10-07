using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class WaitForClickValidation
{
    [MenuItem("Tools/Validation/Wait For Click")]
    public static void RunMenu() => Debug.Log(RunChecks());

    public static string RunChecks()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행하세요.");
        bool contact = false, pressed = false;
        int count = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); count++; };
        Func<IEnumerator> create = () => (IEnumerator)typeof(WaitForClick)
            .GetMethod("WaitForNewPress", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { (Func<bool>)(() => contact), (Func<bool>)(() => pressed) });

        var wait = create();
        check(wait.MoveNext(), "시작 프레임 대기");
        check(wait.MoveNext(), "입력 없이 자동 진행하지 않음");
        contact = pressed = true;
        check(!wait.MoveNext(), "새 터치 시작으로 완료");

        wait = create();
        check(wait.MoveNext(), "이전 확인 터치를 다음 대기에 재사용하지 않음");
        pressed = false;
        for (int i = 0; i < 120; i++)
            if (!wait.MoveNext()) throw new Exception("장시간 터치가 대기를 통과함");
        check(true, "길게 누르기 120 프레임 유지");
        check(wait.MoveNext(), "다른 손가락이 남아 있으면 계속 대기");
        contact = false;
        check(wait.MoveNext(), "모든 손가락 해제 프레임 대기");
        check(wait.MoveNext(), "손 떼기만으로 완료하지 않음");
        contact = true; // Moved/Stationary/Ended/Canceled는 새 입력 아님
        check(wait.MoveNext(), "새 입력이 아닌 접촉은 무시");
        contact = false;
        check(wait.MoveNext(), "취소 이후에도 새 입력 대기");
        contact = pressed = true;
        check(!wait.MoveNext(), "다시 누른 새 터치로 완료");

        contact = false; pressed = true;
        wait = create();
        check(wait.MoveNext(), "시작 프레임의 마우스 Down 무시");
        pressed = false;
        check(wait.MoveNext(), "기존 마우스 클릭 다음 프레임에도 대기");
        pressed = true;
        check(!wait.MoveNext(), "후속 새 마우스 클릭 허용");
        return $"WaitForClick: {count} checks passed (합성 입력, Edit Mode)";
    }
}
