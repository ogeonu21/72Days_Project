using UnityEngine;

public static class JosaUtility
{
    // 종성이 있는지 확인하는 유틸리티 함수
    private static bool HasFinalConsonant(char ch)
    {
        // 한글 범위를 벗어나면 false 반환
        if (ch < '가' || ch > '힣')
        {
            return false;
        }

        // 유니코드 계산을 통해 종성 여부 확인
        int offset = ch - '가';
        int finalConsonantCode = offset % 28;
        return finalConsonantCode != 0;
    }

    public static string GetJosa_이가(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return "이"; 
        }

        char lastChar = word[word.Length - 1];
        return HasFinalConsonant(lastChar) ? "이" : "가";
    }

    public static string GetJosa_은는(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return "은";
        }

        char lastChar = word[word.Length - 1];
        return HasFinalConsonant(lastChar) ? "은" : "는";
    }
}