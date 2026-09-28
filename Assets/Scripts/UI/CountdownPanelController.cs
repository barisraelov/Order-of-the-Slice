using TMPro;
using UnityEngine;

/// <summary>
/// Countdown display: shows "3, 2, 1, GO" and hides for every other state.
/// It does not block input. Countdown does not accept pause.
/// </summary>
public class CountdownPanelController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text countdownText;

    private void OnEnable()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnStateChanged += HandleStateChanged;
        gameManager.OnCountdownTick += HandleTick;
        HandleStateChanged(gameManager.State, gameManager.State);
    }

    private void OnDisable()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnStateChanged -= HandleStateChanged;
        gameManager.OnCountdownTick -= HandleTick;
    }

    private void HandleStateChanged(GameState previous, GameState next)
    {
        SetVisible(next == GameState.Countdown);
    }

    private void HandleTick(string text)
    {
        if (countdownText != null)
        {
            countdownText.text = text;
        }
    }

    private void SetVisible(bool visible)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
