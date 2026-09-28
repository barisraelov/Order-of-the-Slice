using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Brief FEVER! banner and a light playfield wash. Does not change Fever rules.
/// </summary>
public class FeverPresenter : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CanvasGroup bannerGroup;
    [SerializeField] private RectTransform bannerRoot;
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private Image wash;

    private const float PunchIn = 0.12f;
    private const float Hold = 0.38f;
    private const float FadeOut = 0.28f;

    private bool playingBanner;
    private float bannerStartedUnscaled;

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnStateChanged += HandleStateChanged;
        }

        ApplyLook(false, replayBanner: false);
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState previous, GameState next)
    {
        if (next == GameState.Fever)
        {
            ApplyLook(true, replayBanner: previous != GameState.Paused);
            return;
        }

        if (next == GameState.Paused && previous == GameState.Fever)
        {
            return;
        }

        ApplyLook(false, replayBanner: false);
    }

    private void LateUpdate()
    {
        if (!playingBanner)
        {
            return;
        }

        var elapsed = Time.unscaledTime - bannerStartedUnscaled;
        var total = PunchIn + Hold + FadeOut;
        if (elapsed >= total)
        {
            SetBanner(0f, 1f);
            playingBanner = false;
            return;
        }

        float alpha;
        float scale;
        if (elapsed < PunchIn)
        {
            var t = elapsed / PunchIn;
            scale = Mathf.Lerp(0.72f, 1.08f, t);
            alpha = t;
        }
        else if (elapsed < PunchIn + Hold)
        {
            scale = 1f;
            alpha = 1f;
        }
        else
        {
            var t = (elapsed - PunchIn - Hold) / FadeOut;
            scale = 1f;
            alpha = 1f - t;
        }

        SetBanner(alpha, scale);
    }

    private void ApplyLook(bool on, bool replayBanner)
    {
        if (wash != null)
        {
            var color = wash.color;
            color.a = on ? 0.09f : 0f;
            wash.color = color;
            wash.enabled = on;
            wash.raycastTarget = false;
        }

        if (on && replayBanner)
        {
            playingBanner = true;
            bannerStartedUnscaled = Time.unscaledTime;
            if (bannerText != null)
            {
                bannerText.text = "FEVER!";
                bannerText.raycastTarget = false;
            }

            SetBanner(0f, 0.72f);
            return;
        }

        if (!on)
        {
            playingBanner = false;
            SetBanner(0f, 1f);
        }
    }

    private void SetBanner(float alpha, float scale)
    {
        if (bannerGroup != null)
        {
            bannerGroup.alpha = alpha;
            bannerGroup.interactable = false;
            bannerGroup.blocksRaycasts = false;
        }

        if (bannerRoot != null)
        {
            bannerRoot.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
