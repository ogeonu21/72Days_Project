using UnityEngine;

public static class JosaUtility
{
    // Á¾¼ºÀÌ ÀÖ´ÂÁö È®ÀÎÇÏ´Â À¯Æ¿¸®Æ¼ ÇÔ¼ö
    private static bool HasFinalConsonant(char ch)
    {
        // ÇÑ±Û ¹üÀ§¸¦ ¹þ¾î³ª¸é false ¹ÝÈ¯
        if (ch < '°¡' || ch > 'ÆR')
        {
            return false;
        }

        // À¯´ÏÄÚµå °è»êÀ» ÅëÇØ Á¾¼º ¿©ºÎ È®ÀÎ
        int offset = ch - '°¡';
        int finalConsonantCode = offset % 28;
        return finalConsonantCode != 0;
    }

    public static string GetJosa_ÀÌ°¡(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return "ÀÌ"; 
        }

        char lastChar = word[word.Length - 1];
        return HasFinalConsonant(lastChar) ? "ÀÌ" : "°¡";
    }

    public static string GetJosa_Àº´Â(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return "Àº";
        }

        char lastChar = word[word.Length - 1];
        return HasFinalConsonant(lastChar) ? "Àº" : "´Â";
    }
}