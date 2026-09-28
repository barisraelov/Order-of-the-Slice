using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnDirector : MonoBehaviour
{
    private const float ForceFrameTolerance = 0.02f;

    [SerializeField] private GameConfig config;
    [SerializeField] private RecipeManager recipeManager;
    [SerializeField] private PoolManager poolManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Camera worldCamera;

    private Coroutine spawnLoop;
    private float requiredBecameTime;
    private bool spawnedCurrentRequired;
    private IngredientId trackedRequired;

    /// <summary>
    /// When set, throws stay off the required ingredient until the fairness deadline, then force it.
    /// </summary>
    public bool HoldRequiredUntilDue { get; set; }

    /// <summary>Total ingredients and bombs thrown since the last Begin().</summary>
    public int ThrowCount { get; private set; }

    /// <summary>Difficulty-adjusted delay between throws. LiveFever interval is applied in
    /// LiveSpawnInterval while State == Fever; this property stays on the difficulty curve.</summary>
    public float CurrentSpawnInterval { get; private set; }

    /// <summary>Interval actually used for the next throw. Fever swaps this live to
    /// feverSpawnInterval without changing the stored difficulty interval.</summary>
    public float LiveSpawnInterval =>
        gameManager != null && gameManager.State == GameState.Fever
            ? config.feverSpawnInterval
            : (CurrentSpawnInterval > 0f ? CurrentSpawnInterval : config.spawnInterval);

    /// <summary>Difficulty-adjusted extra launch speed. maxRequiredWait is never modified.</summary>
    public float LaunchSpeedBonus { get; private set; }

    public void Begin()
    {
        ThrowCount = 0;
        ApplyDifficulty(recipeManager.CompletedRecipes);
        ResetRequiredTracking();
        if (spawnLoop != null)
        {
            StopCoroutine(spawnLoop);
        }

        spawnLoop = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnLoop != null)
        {
            StopCoroutine(spawnLoop);
            spawnLoop = null;
        }
    }

    public void NotifyRequiredDespawnedUnsliced()
    {
        spawnedCurrentRequired = false;
        requiredBecameTime = Time.time;
    }

    private void OnEnable()
    {
        if (recipeManager != null)
        {
            recipeManager.OnRecipeChanged += HandleRecipeChanged;
            recipeManager.OnCompletedCountChanged += HandleProgressionChanged;
        }
    }

    private void OnDisable()
    {
        if (recipeManager != null)
        {
            recipeManager.OnRecipeChanged -= HandleRecipeChanged;
            recipeManager.OnCompletedCountChanged -= HandleProgressionChanged;
        }
    }

    private void HandleRecipeChanged()
    {
        ApplyDifficulty(recipeManager.CompletedRecipes);
        ResetRequiredTracking();
    }

    private void HandleProgressionChanged()
    {
        ApplyDifficulty(recipeManager.CompletedRecipes);
    }

    public void ApplyDifficulty(int completedRecipes)
    {
        CurrentSpawnInterval = Mathf.Clamp(
            config.spawnInterval - completedRecipes * config.spawnIntervalStep,
            config.minSpawnInterval,
            config.spawnInterval);
        LaunchSpeedBonus = Mathf.Clamp(
            completedRecipes * config.launchSpeedStep,
            0f,
            config.maxLaunchSpeedBonus);
    }

    private void ResetRequiredTracking()
    {
        trackedRequired = recipeManager.CurrentRequired;
        spawnedCurrentRequired = false;
        requiredBecameTime = Time.time;
    }

    // Time.timeScale == 0 during Paused freezes Time.time and Time.deltaTime, so the
    // fairness clock (RemainingUntilRequiredDue) and the wait duration below hold their values
    // automatically across a pause with no explicit check inside this timing math. That alone is
    // NOT enough to stop spawning, though: a coroutine's yield return null still resumes every
    // rendered frame regardless of timeScale, so ThrowOnce() itself must be explicitly gated on
    // IsGameplayActive - continuous numeric drift is handled by scaled time, discrete actions
    // need explicit permission. The outer loop survives Pause (only GameOver ends it) so Resume
    // does not require restarting spawning from scratch the way a bare "while (IsPlaying)" would.
    private System.Collections.IEnumerator SpawnLoop()
    {
        yield return null;
        while (gameManager.State != GameState.GameOver)
        {
            if (!gameManager.IsGameplayActive || recipeManager.IsCompleting)
            {
                yield return null;
                continue;
            }

            ThrowOnce();
            ThrowCount++;
            yield return WaitForNextThrow();
        }
    }

    private System.Collections.IEnumerator WaitForNextThrow()
    {
        while (gameManager.State != GameState.GameOver)
        {
            if (recipeManager.CurrentRequired != trackedRequired)
            {
                ResetRequiredTracking();
            }

            var wait = NextWaitDuration();
            if (wait <= Time.deltaTime)
            {
                yield return null;
                yield break;
            }

            var end = Time.time + wait;
            while (gameManager.State != GameState.GameOver && Time.time < end)
            {
                if (recipeManager.CurrentRequired != trackedRequired)
                {
                    ResetRequiredTracking();
                }

                if (!spawnedCurrentRequired && RemainingUntilRequiredDue() <= Time.deltaTime)
                {
                    yield return null;
                    yield break;
                }

                yield return null;
            }

            yield break;
        }
    }

    private float NextWaitDuration()
    {
        var interval = LiveSpawnInterval;
        if (spawnedCurrentRequired)
        {
            return interval;
        }

        return Mathf.Min(interval, Mathf.Max(0f, RemainingUntilRequiredDue()));
    }

    private float RemainingUntilRequiredDue()
    {
        return config.maxRequiredWait - (Time.time - requiredBecameTime);
    }

    private bool MustForceRequired()
    {
        return !spawnedCurrentRequired && RemainingUntilRequiredDue() <= ForceFrameTolerance;
    }

    private void ThrowOnce()
    {
        if (!gameManager.IsGameplayActive || recipeManager.IsCompleting)
        {
            return;
        }

        if (recipeManager.CurrentRequired != trackedRequired)
        {
            ResetRequiredTracking();
        }

        SliceableObject spawned;
        if (MustForceRequired())
        {
            spawned = poolManager.GetIngredient(recipeManager.CurrentRequired);
            spawnedCurrentRequired = true;
        }
        else if (HoldRequiredUntilDue)
        {
            spawned = poolManager.GetIngredient(PickDecoy());
        }
        else if (Random.value < config.bombChance)
        {
            spawned = poolManager.GetBomb();
        }
        else if (Random.value < config.decoyChance)
        {
            spawned = poolManager.GetIngredient(PickDecoy());
        }
        else
        {
            spawned = poolManager.GetIngredient(recipeManager.CurrentRequired);
            spawnedCurrentRequired = true;
        }

        spawned.Launch(RandomSpawnPosition(), RandomImpulse());
    }

    /// <summary>
    /// Picks a decoy from the complete known ingredient set, never the currently required id.
    /// Can return an ingredient that is not on the current recipe.
    /// </summary>
    public IngredientId PickDecoy()
    {
        var required = (int)recipeManager.CurrentRequired;
        var count = Enum.GetValues(typeof(IngredientId)).Length;
        var pick = Random.Range(0, count - 1);
        var value = pick >= required ? pick + 1 : pick;
        return (IngredientId)value;
    }

    private Vector2 RandomSpawnPosition()
    {
        var cam = worldCamera != null ? worldCamera : Camera.main;
        var z = Mathf.Abs(cam.transform.position.z);
        var left = cam.ViewportToWorldPoint(new Vector3(0.15f, 0f, z));
        var right = cam.ViewportToWorldPoint(new Vector3(0.85f, 0f, z));
        var x = Random.Range(left.x, right.x);
        return new Vector2(x, config.despawnY + 0.5f);
    }

    private Vector2 RandomImpulse()
    {
        var speed = Random.Range(config.launchSpeedRange.x, config.launchSpeedRange.y) + LaunchSpeedBonus;
        var x = Random.Range(-config.horizontalImpulse, config.horizontalImpulse);
        return new Vector2(x, speed);
    }
}
