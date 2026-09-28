using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the six-state lifecycle for the Gameplay scene: MainMenu (exit state only), Countdown,
/// Playing, Fever, Paused, GameOver. Time.timeScale is assigned only through ApplyTimeScale(),
/// called from EnterState() and from a brief bomb hit-stop. Pause still wins (0), GameOver /
/// Countdown / MainMenu restore 1, and hit-stop never leaves a leftover scale. Discrete
/// per-frame actions (spawning, slicing) are gated separately via IsGameplayActive, because
/// coroutines keep ticking every rendered frame even at timeScale 0.
///
/// Score, combo, Fever charge and Fever remaining time live on ScoreFeverSystem.
/// GameManager is the only slice-resolution arbiter and the only Fever lifecycle owner.
/// </summary>
[DefaultExecutionOrder(100)]
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameConfig config;
    [SerializeField] private RecipeManager recipeManager;
    [SerializeField] private SpawnDirector spawnDirector;
    [SerializeField] private PoolManager poolManager;
    [SerializeField] private PointerSliceController pointerSliceController;
    [SerializeField] private ScoreFeverSystem scoreFever;
    [SerializeField] private SlicePresentation slicePresentation;
    [SerializeField] private ScoreFeedback scoreFeedback;

    public event Action<GameState, GameState> OnStateChanged;
    public event Action<int> OnLivesChanged;
    public event Action<string> OnCountdownTick;
    public event Action OnRunStarted;
    public event Action<int, bool> OnGameOver;
    public event Action OnRestartUnlocked;

    public GameState State { get; private set; }
    public int Lives { get; private set; }
    public int Score => scoreFever != null ? scoreFever.Score : 0;
    public ScoreFeverSystem ScoreFever => scoreFever;

    /// <summary>True only during Playing or Fever. Governs whether spawning and slicing may act
    /// this frame.</summary>
    public bool IsGameplayActive => State == GameState.Playing || State == GameState.Fever;

    /// <summary>Legacy alias for SliceableObject. Equivalent to IsGameplayActive.</summary>
    public bool IsPlaying => IsGameplayActive;

    private GameState stateBeforePause;
    private bool restartUnlocked;
    private Coroutine countdownRoutine;
    private Coroutine gameOverRoutine;
    private bool hitStopActive;
    private float hitStopScale = 1f;
    private float hitStopUntilUnscaled;

    private void Awake()
    {
        if (scoreFever == null)
        {
            scoreFever = GetComponent<ScoreFeverSystem>();
        }

        if (scoreFever == null)
        {
            throw new InvalidOperationException(
                "ScoreFeverSystem must be a serialized scene component on GameManager.");
        }

        scoreFever.Bind(config);
        StartCountdown();
    }

    private void Update()
    {
        if (hitStopActive && Time.unscaledTime >= hitStopUntilUnscaled)
        {
            hitStopActive = false;
            ApplyTimeScale();
        }

        TickFeverIfNeeded();

        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        // Android Back is routed through the Input System as Keyboard.current.escapeKey
        // (activeInputHandler is Input System only, so legacy KeyCode.Escape is unavailable).
        // The same key therefore drives both Windows Esc and Android Back.
        switch (State)
        {
            case GameState.Playing:
            case GameState.Fever:
                RequestUserPause();
                break;
            case GameState.Paused:
                Resume();
                break;
            case GameState.GameOver:
                if (restartUnlocked)
                {
                    GoToMainMenu();
                }

                break;
            // Countdown: user pause is not accepted (point 9). MainMenu: no GameManager exists there.
        }
    }

    private void TickFeverIfNeeded()
    {
        if (State != GameState.Fever || scoreFever == null)
        {
            return;
        }

        var hold = recipeManager != null && recipeManager.IsCompleting;
        if (scoreFever.TickFever(hold))
        {
            EnterState(GameState.Playing);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            RequestAutoPause();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            RequestAutoPause();
        }
    }

    private void EnterState(GameState next)
    {
        var previous = State;
        State = next;
        ApplyTimeScale();
        OnStateChanged?.Invoke(previous, next);
    }

    private void ApplyTimeScale()
    {
        if (State == GameState.Paused)
        {
            hitStopActive = false;
            Time.timeScale = 0f;
            return;
        }

        if (State != GameState.Playing && State != GameState.Fever)
        {
            hitStopActive = false;
            Time.timeScale = 1f;
            return;
        }

        Time.timeScale = hitStopActive ? hitStopScale : 1f;
    }

    /// <summary>
    /// Brief unscaled bomb hit-stop. No-ops after Game Over / Pause so those states keep
    /// their contracted timeScale. Recovery is unscaled and always re-applies the state scale.
    /// </summary>
    public void RequestHitStop(float durationUnscaled, float scale)
    {
        if (!IsGameplayActive || durationUnscaled <= 0f)
        {
            return;
        }

        hitStopActive = true;
        hitStopScale = Mathf.Clamp(scale, 0.01f, 1f);
        hitStopUntilUnscaled = Time.unscaledTime + durationUnscaled;
        ApplyTimeScale();
    }

    // ------------------------------------------------------------------------------------------
    // Countdown
    // ------------------------------------------------------------------------------------------

    private void StartCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
        }

        EnterState(GameState.Countdown);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }

        countdownRoutine = StartCoroutine(CountdownRoutine());
    }

    private static readonly string[] CountdownSteps = { "3", "2", "1", "GO" };

    private IEnumerator CountdownRoutine()
    {
        for (var i = 0; i < CountdownSteps.Length; i++)
        {
            OnCountdownTick?.Invoke(CountdownSteps[i]);
            var elapsed = 0f;
            while (elapsed < config.countdownStepSeconds)
            {
                elapsed += Time.deltaTime; // freezes at 0 while Paused
                yield return null;
            }
        }

        countdownRoutine = null;
        BeginPlaying();
    }

    private void BeginPlaying()
    {
        Lives = config.startingLives;
        scoreFever.ResetRun();
        EnterState(GameState.Playing);
        OnLivesChanged?.Invoke(Lives);
        recipeManager.Begin();
        spawnDirector.Begin();
        OnRunStarted?.Invoke();
    }

    // ------------------------------------------------------------------------------------------
    // Pause / Resume
    // ------------------------------------------------------------------------------------------

    /// <summary>User-triggered pause (Esc / Pause button / Android Back). Limited to
    /// Playing/Fever per point 9 - Countdown cannot be paused this way.</summary>
    public void RequestUserPause()
    {
        if (State == GameState.Playing || State == GameState.Fever)
        {
            Pause();
        }
    }

    /// <summary>Focus/background loss. Valid during Countdown as well as Playing/Fever, so
    /// backgrounding mid-countdown freezes it rather than letting it run unattended.</summary>
    public void RequestAutoPause()
    {
        if (State == GameState.Countdown || State == GameState.Playing || State == GameState.Fever)
        {
            Pause();
        }
    }

    private void Pause()
    {
        if (State == GameState.Paused)
        {
            return;
        }

        stateBeforePause = State;
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        // Synchronous and explicit rather than relying on next-frame Update() gating, so a swipe
        // cannot survive into a test or a real frame that runs immediately after Pause() returns.
        pointerSliceController.ClearSwipe();
        EnterState(GameState.Paused);
    }

    /// <summary>Resumes to the exact pre-pause state. A pause during Countdown restarts from 3
    /// so the player is not dropped into a partial count.</summary>
    public void Resume()
    {
        if (State != GameState.Paused)
        {
            return;
        }

        if (stateBeforePause == GameState.Countdown)
        {
            StartCountdown();
        }
        else
        {
            EnterState(stateBeforePause);
        }
    }

    // ------------------------------------------------------------------------------------------
    // Restart / Main Menu
    // ------------------------------------------------------------------------------------------

    /// <summary>Valid from Paused (any time) or GameOver (only after the restart lockout).</summary>
    public void Restart()
    {
        if (!CanLeaveViaButtons())
        {
            return;
        }

        StopGameOverRoutineIfAny();
        poolManager.ReleaseAll();
        if (slicePresentation != null)
        {
            slicePresentation.ReleaseAll();
        }

        if (scoreFeedback != null)
        {
            scoreFeedback.ReleaseAll();
        }

        restartUnlocked = false;
        StartCountdown(); // EnterState(Countdown) resets Time.timeScale to 1
    }

    /// <summary>Valid from Paused (any time) or GameOver (only after the restart lockout).
    /// Enters MainMenu (which resets Time.timeScale to 1) before loading the scene, so a zero
    /// time scale can never survive the transition.</summary>
    public void GoToMainMenu()
    {
        if (!CanLeaveViaButtons())
        {
            return;
        }

        StopGameOverRoutineIfAny();
        poolManager.ReleaseAll();
        if (slicePresentation != null)
        {
            slicePresentation.ReleaseAll();
        }

        if (scoreFeedback != null)
        {
            scoreFeedback.ReleaseAll();
        }

        EnterState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }

    private bool CanLeaveViaButtons()
    {
        if (State == GameState.Paused)
        {
            return true;
        }

        return State == GameState.GameOver && restartUnlocked;
    }

    private void StopGameOverRoutineIfAny()
    {
        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
            gameOverRoutine = null;
        }
    }

    // ------------------------------------------------------------------------------------------
    // Lives / Game Over
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// Single slice arbiter: ingredient correctness, score/combo/charge, recipe advancement,
    /// life loss and Game Over happen here in one order. SliceableObject must not duplicate any
    /// of that state.
    /// </summary>
    public bool ResolveSlice(SliceableObject item)
    {
        if (item == null || item.Sliced || !IsGameplayActive)
        {
            return false;
        }

        if (recipeManager != null && recipeManager.IsCompleting)
        {
            return false;
        }

        item.MarkSliced();

        var position = item.transform.position;
        var id = item.Id;
        var isBomb = item.IsBomb;
        var isCorrect = !isBomb && recipeManager != null && recipeManager.IsCurrentRequired(id);
        if (!isCorrect)
        {
            if (AudioManager.Instance != null)
            {
                if (isBomb)
                {
                    AudioManager.Instance.PlayBomb();
                }
                else
                {
                    AudioManager.Instance.PlayCancel();
                }
            }

            LoseLife();
            if (slicePresentation != null)
            {
                slicePresentation.Play(position, id, isBomb, false);
            }

            poolManager.Release(item);
            return true;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySlice();
        }

        var wasFever = State == GameState.Fever;
        var recipeLength = recipeManager.CurrentRecipeLength;
        var willComplete = recipeManager.CurrentIndex + 1 >= recipeLength;

        scoreFever.ApplyCorrectIngredient(wasFever);
        if (willComplete)
        {
            scoreFever.ApplyCompletionBonus(recipeLength, wasFever);
        }

        if (!wasFever && scoreFever.TryActivateFever())
        {
            EnterState(GameState.Fever);
        }

        recipeManager.NotifyCorrectSlice();
        if (slicePresentation != null)
        {
            slicePresentation.Play(position, id, false, true);
        }

        if (scoreFeedback != null)
        {
            scoreFeedback.ShowIngredient(
                position,
                scoreFever.LastIngredientPoints,
                scoreFever.LastComboMultiplier,
                scoreFever.LastAwardWasFever);
            if (willComplete)
            {
                scoreFeedback.ShowCompletion(position, scoreFever.LastCompletionPoints);
            }
        }

        poolManager.Release(item);
        return true;
    }

    public void LoseLife()
    {
        if (!IsGameplayActive)
        {
            return;
        }

        // Wrong slice, bomb and timeout all lose a life here. Combo resets; charge and active
        // Fever are intentionally left untouched.
        scoreFever.ResetCombo();
        Lives--;
        OnLivesChanged?.Invoke(Lives);
        if (Lives > 0)
        {
            return;
        }

        EnterGameOver();
    }

    private void EnterGameOver()
    {
        spawnDirector.StopSpawning();
        EnterState(GameState.GameOver); // revokes slicing/spawning permission via IsGameplayActive
        pointerSliceController.ClearSwipe();
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        poolManager.ReleaseAll();
        var isNewHighScore = SaveService.TrySetHighScore(Score);
        restartUnlocked = false;
        gameOverRoutine = StartCoroutine(GameOverSequence(isNewHighScore));
    }

    /// <summary>
    /// gameOverDelay and restartLockout both start at t=0 (zero lives). The panel becomes
    /// visible (but fully input-transparent via CanvasGroup) at gameOverDelay. Buttons unlock at
    /// max(gameOverDelay, restartLockout) - the lockout is a FLOOR under the panel's own delay,
    /// not an additive wait, so retuning either value independently stays safe.
    /// </summary>
    private IEnumerator GameOverSequence(bool isNewHighScore)
    {
        yield return new WaitForSeconds(config.gameOverDelay);
        OnGameOver?.Invoke(Score, isNewHighScore);

        var unlockAt = Mathf.Max(config.gameOverDelay, config.restartLockout);
        var remaining = Mathf.Max(0f, unlockAt - config.gameOverDelay);
        yield return new WaitForSeconds(remaining);

        restartUnlocked = true;
        OnRestartUnlocked?.Invoke();
        gameOverRoutine = null;
    }
}
