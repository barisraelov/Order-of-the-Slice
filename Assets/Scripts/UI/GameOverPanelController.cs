using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game Over panel: title, score, Restart, and Main Menu.
/// CanvasGroup starts with both interactable and blocksRaycasts false so the panel is visible
/// but fully input-transparent; both flags flip to true together at the unlock moment. Neither
/// flag alone is sufficient: interactable=false disables the buttons but the panel would still
/// swallow raycasts, while blocksRaycasts=false passes pointer events through but would leave
/// the buttons themselves live.
/// </summary>
public class GameOverPanelController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(HandleRestart);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(HandleMainMenu);
        }

        SetHiddenImmediate();
    }

    private void OnEnable()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnStateChanged += HandleStateChanged;
        gameManager.OnGameOver += HandleGameOver;
        gameManager.OnRestartUnlocked += HandleUnlocked;
    }

    private void OnDisable()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnStateChanged -= HandleStateChanged;
        gameManager.OnGameOver -= HandleGameOver;
        gameManager.OnRestartUnlocked -= HandleUnlocked;
    }

    private void HandleStateChanged(GameState previous, GameState next)
    {
        if (next != GameState.GameOver)
        {
            SetHiddenImmediate();
        }
    }

    private void HandleGameOver(int score, bool isNewHighScore)
    {
        if (titleText != null)
        {
            titleText.text = "GAME OVER";
        }

        if (scoreText != null)
        {
            scoreText.raycastTarget = false;
            scoreText.text = isNewHighScore ? $"Score: {score} (New Best!)" : $"Score: {score}";
        }

        if (bestScoreText != null)
        {
            bestScoreText.raycastTarget = false;
            bestScoreText.text = SaveService.BestScoreLabel;
        }

        if (titleText != null)
        {
            titleText.raycastTarget = false;
        }

        if (group == null)
        {
            return;
        }

        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void HandleUnlocked()
    {
        if (group == null)
        {
            return;
        }

        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void SetHiddenImmediate()
    {
        if (group == null)
        {
            return;
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void HandleRestart()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }

        if (gameManager != null)
        {
            gameManager.Restart();
        }
    }

    private void HandleMainMenu()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClick();
        }

        if (gameManager != null)
        {
            gameManager.GoToMainMenu();
        }
    }
}
