using UnityEngine;

/// <summary>
/// Local high-score persistence over one PlayerPrefs key. GameManager calls
/// TrySetHighScore at zero lives. The key and the "keep the best only" rule stay here.
/// </summary>
public static class SaveService
{
    private const string HighScoreKey = "OrderOfTheSlice.HighScore";

    public static int GetHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    /// <summary>Presentation helper for HUD/menu. Does not change the stored key or semantics.</summary>
    public static string BestScoreLabel => "Best: " + GetHighScore();

    /// <summary>Stores the score only if it beats the current high score. Returns true if it did.</summary>
    public static bool TrySetHighScore(int score)
    {
        if (score <= GetHighScore())
        {
            return false;
        }

        PlayerPrefs.SetInt(HighScoreKey, score);
        PlayerPrefs.Save();
        return true;
    }
}
