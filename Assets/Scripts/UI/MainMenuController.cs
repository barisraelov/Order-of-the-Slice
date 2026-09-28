using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button muteButton;
    [SerializeField] private Image muteIcon;
    [SerializeField] private Sprite muteOnSprite;
    [SerializeField] private Sprite muteOffSprite;
    [SerializeField] private CreditsPanelController creditsPanel;

    public TMP_Text HighScoreText => highScoreText;
    public Button PlayButton => playButton;
    public Button QuitButton => quitButton;

    private void Awake()
    {
        // The MainMenu scene is deliberately stateless and has no GameManager, so nothing else
        // in this scene would ever touch Time.timeScale. This is the second of the two
        // independent guarantees that a zero time scale can never reach the menu: the Gameplay
        // scene's GameManager.EnterState always assigns 1 before Restart or before loading this
        // scene, and this is the floor - even if a future Gameplay path forgot to transition out
        // of Paused before loading, the menu still corrects it on its own first frame.
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }

        if (titleText != null)
        {
            titleText.text = "Order of the Slice";
            titleText.raycastTarget = false;
        }

        if (instructionText != null)
        {
            instructionText.text = "Slice ingredients in recipe order and avoid bombs.";
            instructionText.raycastTarget = false;
        }

        if (highScoreText != null)
        {
            highScoreText.raycastTarget = false;
            highScoreText.text = SaveService.BestScoreLabel;
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(PlayGame);
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(OpenCredits);
        }

        if (muteButton != null)
        {
            muteButton.onClick.AddListener(ToggleMute);
        }

        if (creditsPanel != null)
        {
            creditsPanel.SetBody(CreditsCopy.Body);
        }

        RefreshMuteIcon();

        if (quitButton != null)
        {
            quitButton.gameObject.SetActive(true);
            quitButton.interactable = true;
            quitButton.onClick.AddListener(Quit);
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Quit();
        }
    }

    public void PlayGame()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConfirm();
        }

        SceneManager.LoadScene("Gameplay");
    }

    public void OpenCredits()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClick();
        }

        if (creditsPanel != null)
        {
            creditsPanel.Show();
        }
    }

    public void ToggleMute()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.ToggleMute();
        RefreshMuteIcon();
    }

    public void Quit()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCancel();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshMuteIcon()
    {
        if (muteIcon == null)
        {
            return;
        }

        var muted = AudioManager.Instance != null && AudioManager.Instance.Muted;
        var sprite = muted ? muteOnSprite : muteOffSprite;
        if (sprite != null)
        {
            muteIcon.sprite = sprite;
            muteIcon.preserveAspect = true;
        }

        muteIcon.raycastTarget = false;
    }
}
