/// <summary>
/// The six game states. MainMenu is a real exit state of the Gameplay-scene
/// GameManager: it is entered (which resets Time.timeScale to 1) immediately before
/// SceneManager.LoadScene("MainMenu") is called. The MainMenu scene itself is stateless and
/// has no GameManager.
/// </summary>
public enum GameState
{
    MainMenu,
    Countdown,
    Playing,
    Fever,
    Paused,
    GameOver
}
