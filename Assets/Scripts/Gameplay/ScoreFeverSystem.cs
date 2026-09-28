using System;
using UnityEngine;

/// <summary>
/// Single source of truth for score, combo, combo multiplier, Fever charge and Fever remaining
/// time. GameManager remains the lifecycle arbiter (Playing &lt;-&gt; Fever, Pause, Game Over).
/// </summary>
[DefaultExecutionOrder(-50)]
public class ScoreFeverSystem : MonoBehaviour
{
    private GameConfig config;

    public event Action OnScoreChanged;
    public event Action OnComboChanged;
    public event Action OnFeverChanged;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int FeverCharge { get; private set; }
    public float FeverRemaining { get; private set; }
    public int ActivationCount { get; private set; }
    public int LastIngredientPoints { get; private set; }
    public int LastCompletionPoints { get; private set; }
    public int LastComboMultiplier { get; private set; }
    public bool LastAwardWasFever { get; private set; }

    public int ComboMultiplier
    {
        get
        {
            if (config == null)
            {
                return 1;
            }

            if (Combo >= config.comboTier4At)
            {
                return config.comboMultiplier4;
            }

            if (Combo >= config.comboTier3At)
            {
                return config.comboMultiplier3;
            }

            if (Combo >= config.comboTier2At)
            {
                return config.comboMultiplier2;
            }

            return config.comboMultiplier1;
        }
    }

    public int FeverCapacity => config != null ? config.feverCapacity : 100;
    public float FeverDuration => config != null ? config.feverDuration : 8f;

    public void Bind(GameConfig gameConfig)
    {
        config = gameConfig;
    }

    public void ResetRun()
    {
        Score = 0;
        Combo = 0;
        FeverCharge = 0;
        FeverRemaining = 0f;
        ActivationCount = 0;
        LastIngredientPoints = 0;
        LastCompletionPoints = 0;
        LastComboMultiplier = 1;
        LastAwardWasFever = false;
        OnScoreChanged?.Invoke();
        OnComboChanged?.Invoke();
        OnFeverChanged?.Invoke();
    }

    public void ResetCombo()
    {
        if (Combo == 0)
        {
            return;
        }

        Combo = 0;
        OnComboChanged?.Invoke();
    }

    /// <summary>
    /// Increments combo first, then awards ingredient points at the new multiplier.
    /// Charge is skipped while already in Fever.
    /// </summary>
    public void ApplyCorrectIngredient(bool alreadyInFever)
    {
        Combo++;
        var points = config.ingredientScore * ComboMultiplier;
        if (alreadyInFever)
        {
            points *= config.feverScoreMultiplier;
        }

        LastIngredientPoints = points;
        LastCompletionPoints = 0;
        LastComboMultiplier = ComboMultiplier;
        LastAwardWasFever = alreadyInFever;
        Score += points;
        if (!alreadyInFever)
        {
            FeverCharge += config.feverChargePerIngredient;
        }

        OnComboChanged?.Invoke();
        OnScoreChanged?.Invoke();
        if (!alreadyInFever)
        {
            OnFeverChanged?.Invoke();
        }
    }

    /// <summary>
    /// Recipe completion bonus is 50 × length, not combo-tier multiplied. Doubled only when
    /// already in Fever. Charge is skipped while already in Fever.
    /// </summary>
    public void ApplyCompletionBonus(int recipeLength, bool alreadyInFever)
    {
        var bonus = config.recipeCompletionScorePerIngredient * recipeLength;
        if (alreadyInFever)
        {
            bonus *= config.feverScoreMultiplier;
        }

        LastCompletionPoints = bonus;
        Score += bonus;
        if (!alreadyInFever)
        {
            FeverCharge += config.feverChargePerCompletion;
        }

        OnScoreChanged?.Invoke();
        if (!alreadyInFever)
        {
            OnFeverChanged?.Invoke();
        }
    }

    /// <summary>
    /// Evaluated once after the complete slice transaction. Filling slice used pre-Fever scoring
    /// because alreadyInFever was false for that transaction. Charge resumes from 0.
    /// </summary>
    public bool TryActivateFever()
    {
        if (FeverRemaining > 0f || FeverCharge < config.feverCapacity)
        {
            return false;
        }

        FeverCharge = 0;
        FeverRemaining = config.feverDuration;
        ActivationCount++;
        OnFeverChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Drains remaining Fever time. Caller must skip this while Paused (State != Fever) and pass
    /// hold=true during the recipe-completion transition. Returns true when Fever just expired.
    /// Remaining-time digits are polled by UIController; this does not raise OnFeverChanged every frame.
    /// </summary>
    public bool TickFever(bool hold)
    {
        if (FeverRemaining <= 0f || hold)
        {
            return false;
        }

        FeverRemaining -= Time.deltaTime;
        if (FeverRemaining > 0f)
        {
            return false;
        }

        FeverRemaining = 0f;
        OnFeverChanged?.Invoke();
        return true;
    }
}
