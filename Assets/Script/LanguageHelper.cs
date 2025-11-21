using UnityEngine;

public static class LanguageHelper
{
    /// <summary>
    /// Get translated text for dynamic content that can't use FetchTextController
    /// </summary>
    /// <param name="code">The language code (e.g., "L_7", "L_8", "L_9")</param>
    /// <returns>Translated text or the code if translation not found</returns>
    public static string GetTranslation(string code)
    {
        if (LanguageMan.instance != null)
        {
            return LanguageMan.instance.RequestForText(code);
        }
        return code; // Fallback to code if LanguageMan not available
    }
    
    /// <summary>
    /// Get the "WIN" text with proper formatting and amount
    /// </summary>
    /// <param name="amount">The win amount to display</param>
    /// <returns>Formatted win text with translation</returns>
    public static string GetWinText(float amount)
    {
        string winTranslation = GetTranslation("L_9"); // "WIN" translation
        return $"<font-weight=700><color=#FFD200>{winTranslation}</color> <color=white>{amount:N2}</color></font-weight>";
    }
    
    /// <summary>
    /// Get just the amount formatted without WIN text
    /// </summary>
    /// <param name="amount">The amount to display</param>
    /// <returns>Formatted amount</returns>
    public static string GetAmountText(float amount)
    {
        return $"{amount:N2}";
    }
}