using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Order of the Slice/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Launch")]
    public Vector2 launchSpeedRange = new Vector2(11f, 15f);
    public float spawnInterval = 0.85f;
    public float horizontalImpulse = 2.5f;
    public float despawnY = -7f;

    [Header("Fairness and hazards")]
    [Range(0f, 1f)] public float bombChance = 0.08f;
    [Range(0f, 1f)] public float decoyChance = 0.45f;
    public float maxRequiredWait = 1.4f;

    [Header("Recipe")]
    public float recipeTime = 14f;
    public int startingLives = 3;

    [Header("Slice")]
    public float minimumSliceSpeed = 700f;
    public float referenceWidth = 1920f;
    public float referenceHeight = 1080f;

    [Header("Pools")]
    public int ingredientPrewarm = 8;
    public int bombPrewarm = 4;
    public int ingredientMax = 24;
    public int bombMax = 12;

    [Header("Lifecycle timing")]
    [Tooltip("Duration of each Countdown step (3, 2, 1, GO).")]
    public float countdownStepSeconds = 0.75f;
    [Tooltip("Delay after zero lives before the Game Over panel becomes visible.")]
    public float gameOverDelay = 1.0f;
    [Tooltip("Minimum time after zero lives before Game Over buttons accept input. " +
             "Acts as a floor against gameOverDelay, not an additive delay.")]
    public float restartLockout = 0.75f;
    [Tooltip("Hold duration after a recipe completes before the next recipe begins. " +
             "Slicing stays blocked; spawning and Fever time are held; physics keeps running.")]
    public float recipeTransitionSeconds = 0.5f;

    [Header("Progression and difficulty")]
    [Tooltip("Completed-recipe counts that unlock length 4 then length 5. x=3, y=6.")]
    public Vector2Int recipeLengthThresholds = new Vector2Int(3, 6);
    [Tooltip("Spawn interval subtracted per completed recipe.")]
    public float spawnIntervalStep = 0.05f;
    [Tooltip("Lowest allowed spawn interval. maxRequiredWait is never scaled.")]
    public float minSpawnInterval = 0.55f;
    [Tooltip("Launch-speed bonus added per completed recipe.")]
    public float launchSpeedStep = 0.35f;
    [Tooltip("Cap on the launch-speed bonus.")]
    public float maxLaunchSpeedBonus = 2f;

    [Header("Score, combo, Fever")]
    [Tooltip("Points for a correct ingredient before combo and Fever multipliers.")]
    public int ingredientScore = 10;
    [Tooltip("Completion bonus is this value multiplied by recipe length, not by combo tier.")]
    public int recipeCompletionScorePerIngredient = 50;
    [Tooltip("Combo count at which the x2 tier begins (5th correct slice).")]
    public int comboTier2At = 5;
    [Tooltip("Combo count at which the x3 tier begins.")]
    public int comboTier3At = 10;
    [Tooltip("Combo count at which the x4 tier begins.")]
    public int comboTier4At = 15;
    public int comboMultiplier1 = 1;
    public int comboMultiplier2 = 2;
    public int comboMultiplier3 = 3;
    public int comboMultiplier4 = 4;
    [Tooltip("Fever charge capacity. Activation is evaluated once after the slice transaction.")]
    public int feverCapacity = 100;
    [Tooltip("Charge added for a correct ingredient while Fever is not active.")]
    public int feverChargePerIngredient = 8;
    [Tooltip("Extra charge on recipe completion, in addition to the final ingredient charge.")]
    public int feverChargePerCompletion = 16;
    [Tooltip("Fever duration in scaled seconds.")]
    public float feverDuration = 8f;
    [Tooltip("Spawn interval used live while State == Fever. Outside Fever, difficulty interval is used.")]
    public float feverSpawnInterval = 0.55f;
    [Tooltip("Score multiplier applied to ingredient points and completion bonus while already in Fever.")]
    public int feverScoreMultiplier = 2;

    public int RecipeLengthForCompletedCount(int completed)
    {
        if (completed < recipeLengthThresholds.x)
        {
            return 3;
        }

        if (completed < recipeLengthThresholds.y)
        {
            return 4;
        }

        return 5;
    }
}
