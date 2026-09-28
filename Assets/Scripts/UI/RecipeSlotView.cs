using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One HUD recipe entry. Icon sits above the name so they never overlap. State is shown with
/// a label or check plus a border/scale, not by color alone.
/// </summary>
public class RecipeSlotView : MonoBehaviour
{
    [SerializeField] private Image panel;
    [SerializeField] private Image border;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Image checkMark;

    public TMP_Text NameLabel => nameLabel;
    public Image Icon => icon;

    public void Bind(Image panelImage, Image borderImage, Image iconImage, TMP_Text name, TMP_Text status, Image check)
    {
        panel = panelImage;
        border = borderImage;
        icon = iconImage;
        nameLabel = name;
        statusLabel = status;
        checkMark = check;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show(Sprite sprite, string ingredientName, RecipeSlotState state)
    {
        gameObject.SetActive(true);
        transform.localScale = state == RecipeSlotState.Current ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;

        if (icon != null)
        {
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.color = Color.white;
        }

        if (nameLabel != null)
        {
            nameLabel.raycastTarget = false;
            nameLabel.text = ingredientName;
            nameLabel.color = state switch
            {
                RecipeSlotState.Current => new Color(1f, 0.93f, 0.62f, 1f),
                RecipeSlotState.Completed => new Color(0.97f, 0.95f, 0.88f, 1f),
                _ => new Color(0.90f, 0.92f, 0.95f, 1f)
            };
        }

        if (statusLabel != null)
        {
            statusLabel.raycastTarget = false;
            statusLabel.text = string.Empty;
            statusLabel.gameObject.SetActive(false);
        }

        if (checkMark != null)
        {
            checkMark.raycastTarget = false;
            checkMark.enabled = state == RecipeSlotState.Completed;
            checkMark.color = new Color(0.45f, 0.85f, 0.4f, 1f);
        }

        if (border != null)
        {
            border.raycastTarget = false;
            border.color = state switch
            {
                RecipeSlotState.Current => new Color(1f, 0.62f, 0.18f, 1f),
                RecipeSlotState.Completed => new Color(0.28f, 0.72f, 0.32f, 1f),
                _ => new Color(0.72f, 0.74f, 0.78f, 0.7f)
            };
        }

        if (panel != null)
        {
            panel.raycastTarget = false;
        }
    }
}

public enum RecipeSlotState
{
    Future = 0,
    Current = 1,
    Completed = 2
}
