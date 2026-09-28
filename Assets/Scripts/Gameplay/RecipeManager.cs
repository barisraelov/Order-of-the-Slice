using System;
using System.Collections;
using UnityEngine;

public class RecipeManager : MonoBehaviour
{
    [SerializeField] private GameConfig config;
    [SerializeField] private RecipeDefinition recipe;
    [SerializeField] private RecipeDefinition[] library;
    [SerializeField] private GameManager gameManager;

    public event Action OnRecipeChanged;
    public event Action OnCompletedCountChanged;

    public RecipeDefinition Recipe => recipe;
    public RecipeDefinition[] Library => library;
    public int CurrentIndex { get; private set; }
    public float RemainingTime { get; private set; }
    public bool IsCompleting { get; private set; }
    public int CompletedRecipes { get; private set; }
    public IngredientId CurrentRequired => recipe.orderedIngredients[CurrentIndex];

    public bool IsCurrentRequired(IngredientId id)
    {
        return !IsCompleting && id == CurrentRequired;
    }

    public int CurrentRecipeLength => recipe != null && recipe.orderedIngredients != null
        ? recipe.orderedIngredients.Length
        : 0;

    public void Begin()
    {
        StopAllCoroutines();
        IsCompleting = false;
        CompletedRecipes = 0;
        OnCompletedCountChanged?.Invoke();
        SelectAndStartRecipe(previous: null);
        StartCoroutine(TimerLoop());
    }

    public void NotifyCorrectSlice()
    {
        if (!gameManager.IsPlaying || IsCompleting)
        {
            return;
        }

        if (CurrentIndex + 1 >= recipe.orderedIngredients.Length)
        {
            IsCompleting = true;
            CompletedRecipes++;
            OnCompletedCountChanged?.Invoke();
            StartCoroutine(CompleteThenRestart());
            return;
        }

        CurrentIndex++;
        OnRecipeChanged?.Invoke();
    }

    /// <summary>Test hook: force the completed-recipe count, then pick a recipe for that bucket.</summary>
    public void DebugSetCompletedRecipes(int count)
    {
        CompletedRecipes = Mathf.Max(0, count);
        OnCompletedCountChanged?.Invoke();
        SelectAndStartRecipe(previous: recipe);
    }

    /// <summary>Test hook: re-roll the current bucket, excluding the current recipe when possible.</summary>
    public void DebugReselectCurrentBucket()
    {
        SelectAndStartRecipe(previous: recipe);
    }

    private void SelectAndStartRecipe(RecipeDefinition previous)
    {
        recipe = PickRecipe(config.RecipeLengthForCompletedCount(CompletedRecipes), previous);
        CurrentIndex = 0;
        RemainingTime = config.recipeTime;
        OnRecipeChanged?.Invoke();
    }

    private RecipeDefinition PickRecipe(int length, RecipeDefinition previous)
    {
        var eligible = 0;
        RecipeDefinition fallback = null;
        for (var i = 0; i < library.Length; i++)
        {
            var candidate = library[i];
            if (!MatchesLength(candidate, length))
            {
                continue;
            }

            fallback = candidate;
            if (previous != null && candidate == previous)
            {
                continue;
            }

            eligible++;
        }

        if (eligible == 0)
        {
            if (fallback == null)
            {
                throw new InvalidOperationException("No RecipeDefinition of length " + length + " in the library.");
            }

            return fallback;
        }

        var pick = UnityEngine.Random.Range(0, eligible);
        for (var i = 0; i < library.Length; i++)
        {
            var candidate = library[i];
            if (!MatchesLength(candidate, length))
            {
                continue;
            }

            if (previous != null && candidate == previous)
            {
                continue;
            }

            if (pick == 0)
            {
                return candidate;
            }

            pick--;
        }

        return fallback;
    }

    private static bool MatchesLength(RecipeDefinition candidate, int length)
    {
        return candidate != null
            && candidate.orderedIngredients != null
            && candidate.orderedIngredients.Length == length;
    }

    // The outer loop survives a Pause. Time.deltaTime is scaled, so it is exactly 0
    // while Time.timeScale == 0 (Paused) - the timer freezes automatically with no explicit
    // pause check needed for the countdown itself. What DOES need an explicit check is whether
    // the loop should decrement at all right now (IsGameplayActive covers Countdown, GameOver
    // and MainMenu, none of which should ever tick the recipe timer). The loop only terminates
    // permanently at GameOver; Paused no longer kills the coroutine the way "while (IsPlaying)"
    // used to, which would have left the timer dead forever after a Resume.
    private IEnumerator TimerLoop()
    {
        while (gameManager.State != GameState.GameOver)
        {
            if (gameManager.IsGameplayActive && !IsCompleting)
            {
                RemainingTime -= Time.deltaTime;
                if (RemainingTime <= 0f)
                {
                    gameManager.LoseLife();
                    if (gameManager.State == GameState.GameOver)
                    {
                        yield break;
                    }

                    // Timeout/restart must not increment completedRecipes. Re-roll the same
                    // length bucket, excluding the recipe that just expired when another exists.
                    SelectAndStartRecipe(previous: recipe);
                }
            }

            yield return null;
        }
    }

    // Decision B (locked): no ReleaseAll here. Airborne objects survive recipe completion and
    // may become wrong-ingredient traps under the new recipe - that is deliberate decoy
    // pressure, not a bug. WaitForSeconds (scaled) replaces WaitForSecondsRealtime so a Pause
    // mid-transition holds it exactly like every other gameplay clock.
    private IEnumerator CompleteThenRestart()
    {
        var previous = recipe;
        yield return new WaitForSeconds(config.recipeTransitionSeconds);
        if (gameManager.State != GameState.GameOver)
        {
            SelectAndStartRecipe(previous);
        }

        IsCompleting = false;
    }
}
