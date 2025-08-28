using UnityEngine;

public static class JosaUtility
{
    // ¹ŞÄ§ÀÌ ÀÖ´ÂÁö È®ÀÎÇÏ´Â ÇïÆÛ ÇÔ¼ö
    private static bool HasFinalConsonant(char ch)
    {
        // ÇÑ±Û ¹üÀ§¸¦ ¹ş¾î³ª¸é false ¹İÈ¯
        if (ch < '°¡' || ch > 'ÆR')
        {
            return false;
        }

        // À¯´ÏÄÚµå °ªÀ» ±âÁØÀ¸·Î ¹ŞÄ§ À¯¹« ÆÇ´Ü
        int offset = ch - '°¡';
        int finalConsonantCode = offset % 28;
        return finalConsonantCode != 0;
    }

    /// <summary>
    /// ÀÔ·ÂµÈ ´Ü¾î¿¡ µû¶ó 'ÀÌ' ¶Ç´Â '°¡' Á¶»ç¸¦ ¹İÈ¯ÇÕ´Ï´Ù.
    /// </summary>
    /// <param name="word">Á¶»ç¸¦ ºÙÀÏ ´Ü¾î</param>
    /// <returns>¹ŞÄ§ÀÌ ÀÖÀ¸¸é 'ÀÌ', ¾øÀ¸¸é '°¡'</returns>
    public static string GetJosa_ÀÌ°¡(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return "ÀÌ"; // ¶Ç´Â "°¡" µî ±âº»°ª
        }

        char lastChar = word[word.Length - 1];
        return HasFinalConsonant(lastChar) ? "ÀÌ" : "°¡";
    }

    /// <summary>
    /// ÀÔ·ÂµÈ ´Ü¾î¿¡ µû¶ó 'Àº' ¶Ç´Â '´Â' Á¶»ç¸¦ ¹İÈ¯ÇÕ´Ï´Ù.
    /// </summary>
    /// <param name="word">Á¶»ç¸¦ ºÙÀÏ ´Ü¾î</param>
    /// <returns>¹ŞÄ§ÀÌ ÀÖÀ¸¸é 'Àº', ¾øÀ¸¸é '´Â'</returns>
    public static string GetJosa_Àº´Â(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return "Àº"; // ¶Ç´Â "´Â" µî ±âº»°ª
        }

        char lastChar = word[word.Length - 1];
        return HasFinalConsonant(lastChar) ? "Àº" : "´Â";
    }
}