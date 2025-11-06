using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//랜덤 값 판정, 계산 함수 모음 클래스.
public static class CalculateFunction
{
    public static bool Roll(float f)
    {
        return f >= UnityEngine.Random.Range(0f, 1f);
    }
}
