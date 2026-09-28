using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu Credits overlay. Hidden until opened. Does not change scene navigation.
/// </summary>
public class CreditsPanelController : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;

    public Button CloseButton => closeButton;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Close()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClick();
        }

        HideSilent();
    }

    public void SetBody(string text)
    {
        if (bodyText != null)
        {
            bodyText.raycastTarget = false;
            bodyText.text = text;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    public void Hide()
    {
        HideSilent();
    }

    private void HideSilent()
    {
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }
}
