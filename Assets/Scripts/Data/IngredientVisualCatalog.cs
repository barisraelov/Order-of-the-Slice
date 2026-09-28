using UnityEngine;

/// <summary>
/// Single source of truth for ingredient and bomb presentation. Gameplay and HUD look up
/// sprites and effect tints here instead of duplicating IngredientId switches.
/// SlicePresentation reads mapped halves and tints; this catalog does not spawn them.
/// </summary>
[CreateAssetMenu(fileName = "IngredientVisualCatalog", menuName = "Order of the Slice/Ingredient Visual Catalog")]
public class IngredientVisualCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public IngredientId id;
        public Sprite whole;
        public Sprite halfA;
        public Sprite halfB;
        public Sprite hudIcon;
        public Color effectTint = Color.white;
    }

    [SerializeField] private Entry[] ingredients = System.Array.Empty<Entry>();
    [SerializeField] private Sprite bombSprite;
    [SerializeField] private Color bombTint = Color.white;
    [SerializeField] private float visualWorldSize = 0.9f;

    public Sprite BombSprite => bombSprite;
    public Color BombTint => bombTint;
    public float VisualWorldSize => visualWorldSize;

    public bool TryGet(IngredientId id, out Entry entry)
    {
        if (ingredients != null)
        {
            for (var i = 0; i < ingredients.Length; i++)
            {
                var candidate = ingredients[i];
                if (candidate != null && candidate.id == id)
                {
                    entry = candidate;
                    return true;
                }
            }
        }

        entry = null;
        return false;
    }

    public Sprite GetWhole(IngredientId id)
    {
        return TryGet(id, out var entry) ? entry.whole : null;
    }

    public Sprite GetHudIcon(IngredientId id)
    {
        if (!TryGet(id, out var entry) || entry == null)
        {
            return null;
        }

        return entry.hudIcon != null ? entry.hudIcon : entry.whole;
    }

    public Color GetEffectTint(IngredientId id)
    {
        return TryGet(id, out var entry) && entry != null ? entry.effectTint : Color.white;
    }

    public bool TryGetHalves(IngredientId id, out Sprite halfA, out Sprite halfB)
    {
        if (TryGet(id, out var entry) && entry != null)
        {
            halfA = entry.halfA;
            halfB = entry.halfB;
            return halfA != null && halfB != null;
        }

        halfA = null;
        halfB = null;
        return false;
    }
}
