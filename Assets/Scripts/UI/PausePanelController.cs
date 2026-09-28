using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause panel: Resume, Restart, and Main Menu. Visible only while Paused.
/// </summary>
public class PausePanelController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(HandleResume);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(HandleRestart);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(HandleMainMenu);
        }
    }

    private void OnEnable()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnStateChanged += HandleStateChanged;
        HandleStateChanged(gameManager.State, gameManager.State);
    }

    private void OnDisable()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState previous, GameState next)
    {
        var visible = next == GameState.Paused;
        if (group == null)
        {
            return;
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void HandleResume()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }

        if (gameManager != null)
        {
            gameManager.Resume();
        }
    }

    private void HandleRestart()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClick();
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
