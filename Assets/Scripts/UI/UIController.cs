using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gameplay HUD subscriber. GameManager, RecipeManager and ScoreFeverSystem remain the sources
/// of state. Buttons call GameManager only. Discrete labels refresh from events; the recipe timer
/// and Fever remaining time poll with a cached last-displayed value.
/// </summary>
[DefaultExecutionOrder(200)]
public class UIController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RecipeManager recipeManager;
    [SerializeField] private ScoreFeverSystem scoreFever;

    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text[] recipeSlots;
    [SerializeField] private Image[] recipeIcons;
    [SerializeField] private RecipeSlotView[] recipeSlotViews;
    [SerializeField] private IngredientVisualCatalog catalog;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text feverText;
    [SerializeField] private Image feverFill;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button muteButton;
    [SerializeField] private Image muteIcon;
    [SerializeField] private Sprite muteOnSprite;
    [SerializeField] private Sprite muteOffSprite;

    private int lastTimerShown = int.MinValue;
    private string lastFeverShown;
    private bool subscribed;
    private int lastComboMultiplier = 1;
    private float comboPunchUntil;
    private Vector3 comboBaseScale = Vector3.one;
    private static readonly Color FeverChargeFill = new Color(0.92f, 0.68f, 0.22f, 1f);
    private static readonly Color FeverActiveFill = new Color(1f, 0.46f, 0.14f, 1f);
    private static readonly Color ComboPunchColor = new Color(1f, 0.82f, 0.38f, 1f);
    private static readonly Color ComboIdleColor = new Color(0.97f, 0.95f, 0.88f, 1f);

    public TMP_Text LivesText => livesText;
    public TMP_Text[] RecipeSlots => recipeSlots;
    public TMP_Text TimerText => timerText;
    public TMP_Text ScoreText => scoreText;
    public TMP_Text ComboText => comboText;
    public TMP_Text FeverText => feverText;
    public Image FeverFill => feverFill;
    public Button PauseButton => pauseButton;

    private void Awake()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(HandlePause);
        }

        if (muteButton != null)
        {
            muteButton.onClick.AddListener(HandleMute);
        }

        SetPassiveRaycasts();
        RefreshMuteIcon();
        if (comboText != null)
        {
            comboBaseScale = comboText.transform.localScale;
        }
    }

    private void OnEnable()
    {
        Subscribe();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMuteChanged += HandleMuteChanged;
        }

        RefreshAll();
        RefreshMuteIcon();
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMuteChanged -= HandleMuteChanged;
        }

        Unsubscribe();
    }

    private void HandleMuteChanged(bool muted)
    {
        RefreshMuteIcon();
    }

    private void Update()
    {
        RefreshTimerIfChanged();
        RefreshFeverIfChanged();
        TickComboPunch();
        TickFeverPulse();
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        if (gameManager != null)
        {
            gameManager.OnLivesChanged += HandleLivesChanged;
            gameManager.OnStateChanged += HandleStateChanged;
            gameManager.OnRunStarted += RefreshAll;
        }

        if (recipeManager != null)
        {
            recipeManager.OnRecipeChanged += HandleRecipeChanged;
        }

        if (scoreFever != null)
        {
            scoreFever.OnScoreChanged += HandleScoreChanged;
            scoreFever.OnComboChanged += HandleComboChanged;
            scoreFever.OnFeverChanged += HandleFeverChanged;
        }

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (gameManager != null)
        {
            gameManager.OnLivesChanged -= HandleLivesChanged;
            gameManager.OnStateChanged -= HandleStateChanged;
            gameManager.OnRunStarted -= RefreshAll;
        }

        if (recipeManager != null)
        {
            recipeManager.OnRecipeChanged -= HandleRecipeChanged;
        }

        if (scoreFever != null)
        {
            scoreFever.OnScoreChanged -= HandleScoreChanged;
            scoreFever.OnComboChanged -= HandleComboChanged;
            scoreFever.OnFeverChanged -= HandleFeverChanged;
        }

        subscribed = false;
    }

    private void HandlePause()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClick();
        }

        if (gameManager != null)
        {
            gameManager.RequestUserPause();
        }
    }

    private void HandleMute()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.ToggleMute();
        RefreshMuteIcon();
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

    private void HandleLivesChanged(int lives)
    {
        RefreshLives();
    }

    private void HandleStateChanged(GameState previous, GameState next)
    {
        RefreshPauseButton(next);
        RefreshFeverIfChanged(force: true);
    }

    private void HandleRecipeChanged()
    {
        RefreshRecipe();
        lastTimerShown = int.MinValue;
        RefreshTimerIfChanged();
    }

    private void HandleScoreChanged()
    {
        RefreshScore();
    }

    private void HandleComboChanged()
    {
        RefreshCombo();
    }

    private void HandleFeverChanged()
    {
        RefreshFeverIfChanged(force: true);
    }

    public void RefreshAll()
    {
        SetPassiveRaycasts();
        RefreshLives();
        RefreshRecipe();
        RefreshScore();
        RefreshCombo();
        lastTimerShown = int.MinValue;
        lastFeverShown = null;
        RefreshTimerIfChanged();
        RefreshFeverIfChanged(force: true);
        if (gameManager != null)
        {
            RefreshPauseButton(gameManager.State);
        }
    }

    private void RefreshLives()
    {
        if (livesText == null || gameManager == null)
        {
            return;
        }

        livesText.text = "Lives: " + gameManager.Lives;
    }

    private void RefreshRecipe()
    {
        if (recipeSlots == null || recipeManager == null || recipeManager.Recipe == null)
        {
            return;
        }

        var sequence = recipeManager.Recipe.orderedIngredients;
        for (var i = 0; i < recipeSlots.Length; i++)
        {
            if (recipeSlots[i] == null)
            {
                continue;
            }

            recipeSlots[i].raycastTarget = false;
            if (sequence == null || i >= sequence.Length)
            {
                recipeSlots[i].text = string.Empty;
                if (recipeSlotViews != null && i < recipeSlotViews.Length && recipeSlotViews[i] != null)
                {
                    recipeSlotViews[i].Hide();
                }
                else
                {
                    SetRecipeIcon(i, null);
                }

                continue;
            }

            var state = i < recipeManager.CurrentIndex
                ? RecipeSlotState.Completed
                : i == recipeManager.CurrentIndex
                    ? RecipeSlotState.Current
                    : RecipeSlotState.Future;
            var icon = catalog != null ? catalog.GetHudIcon(sequence[i]) : null;
            recipeSlots[i].text = sequence[i].ToString();
            if (recipeSlotViews != null && i < recipeSlotViews.Length && recipeSlotViews[i] != null)
            {
                recipeSlotViews[i].Show(icon, sequence[i].ToString(), state);
            }
            else
            {
                SetRecipeIcon(i, icon);
            }
        }
    }

    private void SetRecipeIcon(int index, Sprite sprite)
    {
        if (recipeIcons == null || index < 0 || index >= recipeIcons.Length || recipeIcons[index] == null)
        {
            return;
        }

        var icon = recipeIcons[index];
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        icon.color = Color.white;
    }

    private void RefreshScore()
    {
        if (scoreText == null || scoreFever == null)
        {
            return;
        }

        scoreText.text = "Score: " + scoreFever.Score;
    }

    private void RefreshCombo()
    {
        if (comboText == null || scoreFever == null)
        {
            return;
        }

        var multiplier = scoreFever.ComboMultiplier;
        comboText.text = "Combo: " + scoreFever.Combo + " x" + multiplier;
        if (multiplier > lastComboMultiplier)
        {
            comboPunchUntil = Time.unscaledTime + 0.28f;
            comboText.color = ComboPunchColor;
        }

        lastComboMultiplier = multiplier;
        if (scoreFever.Combo == 0)
        {
            lastComboMultiplier = 1;
        }
    }

    private void RefreshTimerIfChanged()
    {
        if (timerText == null || recipeManager == null)
        {
            return;
        }

        var shown = Mathf.CeilToInt(Mathf.Max(0f, recipeManager.RemainingTime));
        if (shown == lastTimerShown)
        {
            return;
        }

        lastTimerShown = shown;
        timerText.text = "Time: " + shown;
        timerText.raycastTarget = false;
    }

    private void RefreshFeverIfChanged(bool force = false)
    {
        if (scoreFever == null)
        {
            return;
        }

        string shown;
        var fill = 0f;
        var inFever = gameManager != null && gameManager.State == GameState.Fever;
        if (inFever)
        {
            shown = "Fever: " + scoreFever.FeverRemaining.ToString("0.0") + "s";
            var duration = scoreFever.FeverDuration;
            fill = duration > 0f ? Mathf.Clamp01(scoreFever.FeverRemaining / duration) : 0f;
        }
        else
        {
            shown = "Fever: " + scoreFever.FeverCharge + "/" + scoreFever.FeverCapacity;
            var capacity = scoreFever.FeverCapacity;
            fill = capacity > 0 ? Mathf.Clamp01(scoreFever.FeverCharge / (float)capacity) : 0f;
        }

        if (!force && shown == lastFeverShown)
        {
            return;
        }

        lastFeverShown = shown;
        if (feverText != null)
        {
            feverText.raycastTarget = false;
            feverText.text = shown;
        }

        if (feverFill != null)
        {
            feverFill.raycastTarget = false;
            feverFill.fillAmount = fill;
            feverFill.color = inFever ? FeverActiveFill : FeverChargeFill;
        }
    }

    private void TickComboPunch()
    {
        if (comboText == null)
        {
            return;
        }

        if (Time.unscaledTime >= comboPunchUntil)
        {
            comboText.transform.localScale = comboBaseScale;
            if (comboText.color != ComboIdleColor && Time.unscaledTime >= comboPunchUntil)
            {
                comboText.color = ComboIdleColor;
            }

            return;
        }

        var t = 1f - ((comboPunchUntil - Time.unscaledTime) / 0.28f);
        var punch = 1f + (0.14f * (1f - t));
        comboText.transform.localScale = comboBaseScale * punch;
    }

    private void TickFeverPulse()
    {
        if (feverFill == null || scoreFever == null)
        {
            return;
        }

        var inFever = gameManager != null && gameManager.State == GameState.Fever;
        var nearFull = !inFever && scoreFever.FeverCapacity > 0 &&
                       scoreFever.FeverCharge >= scoreFever.FeverCapacity * 0.9f;
        if (!inFever && !nearFull)
        {
            feverFill.transform.localScale = Vector3.one;
            return;
        }

        var pulse = 1f + (0.06f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f)));
        feverFill.transform.localScale = new Vector3(pulse, pulse, 1f);
    }

    private void RefreshPauseButton(GameState state)
    {
        if (pauseButton == null)
        {
            return;
        }

        var visible = state == GameState.Playing || state == GameState.Fever;
        pauseButton.gameObject.SetActive(visible);
        pauseButton.interactable = visible;
    }

    private void SetPassiveRaycasts()
    {
        if (livesText != null) livesText.raycastTarget = false;
        if (timerText != null) timerText.raycastTarget = false;
        if (scoreText != null) scoreText.raycastTarget = false;
        if (comboText != null) comboText.raycastTarget = false;
        if (feverText != null) feverText.raycastTarget = false;
        if (feverFill != null) feverFill.raycastTarget = false;
        if (recipeSlots == null)
        {
            return;
        }

        for (var i = 0; i < recipeSlots.Length; i++)
        {
            if (recipeSlots[i] != null)
            {
                recipeSlots[i].raycastTarget = false;
            }
        }

        if (recipeIcons == null)
        {
            return;
        }

        for (var i = 0; i < recipeIcons.Length; i++)
        {
            if (recipeIcons[i] != null)
            {
                recipeIcons[i].raycastTarget = false;
            }
        }
    }
}
