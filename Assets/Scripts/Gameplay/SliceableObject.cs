using TMPro;
using UnityEngine;

/// <summary>
/// One airborne ingredient or bomb. Accepts a single slice, then returns to the pool.
/// </summary>
public class SliceableObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private IngredientVisualCatalog catalog;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Rigidbody2D body;

    public IngredientId Id { get; private set; }
    public bool IsBomb { get; private set; }
    public bool Sliced { get; private set; }

    private bool inFlight;
    private GameManager gameManager;
    private RecipeManager recipeManager;
    private PoolManager poolManager;
    private SpawnDirector spawnDirector;
    private GameConfig config;
    private bool bound;

    public void Bind(
        GameManager game,
        RecipeManager recipes,
        PoolManager pool,
        SpawnDirector director,
        GameConfig gameConfig)
    {
        gameManager = game;
        recipeManager = recipes;
        poolManager = pool;
        spawnDirector = director;
        config = gameConfig;
        bound = true;
    }

    public void ConfigureIngredient(IngredientId id)
    {
        Id = id;
        IsBomb = false;
        Sliced = false;
        ApplyCatalogVisual(catalog != null ? catalog.GetWhole(id) : null, Color.white);
    }

    public void ConfigureBomb()
    {
        Id = IngredientId.Tomato;
        IsBomb = true;
        Sliced = false;
        var bombSprite = catalog != null ? catalog.BombSprite : null;
        ApplyCatalogVisual(bombSprite, Color.white);
    }

    public void Launch(Vector2 position, Vector2 impulse)
    {
        inFlight = true;
        transform.position = position;
        transform.rotation = Quaternion.identity;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.AddForce(impulse, ForceMode2D.Impulse);
        }
    }

    public bool TryAcceptSlice()
    {
        if (Sliced || !bound || gameManager == null || !gameManager.IsPlaying)
        {
            return false;
        }

        if (recipeManager != null && recipeManager.IsCompleting)
        {
            return false;
        }

        return gameManager.ResolveSlice(this);
    }

    public void MarkSliced()
    {
        Sliced = true;
    }

    public void ResetForPool()
    {
        Sliced = false;
        inFlight = false;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private void Update()
    {
        if (!bound || !inFlight || !isActiveAndEnabled || Sliced || config == null)
        {
            return;
        }

        if (transform.position.y > config.despawnY)
        {
            return;
        }

        var wasRequired = !IsBomb && recipeManager != null && recipeManager.IsCurrentRequired(Id);
        poolManager.Release(this);
        if (wasRequired && spawnDirector != null)
        {
            spawnDirector.NotifyRequiredDespawnedUnsliced();
        }
    }

    private void ApplyCatalogVisual(Sprite sprite, Color color)
    {
        if (label != null)
        {
            label.gameObject.SetActive(false);
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        FitVisual(sprite);
    }

    private void FitVisual(Sprite sprite)
    {
        if (visualRoot == null || visualRoot == transform || sprite == null || catalog == null)
        {
            return;
        }

        var size = sprite.bounds.size;
        var max = Mathf.Max(size.x, size.y);
        if (max <= 0f)
        {
            return;
        }

        var scale = catalog.VisualWorldSize / max;
        visualRoot.localScale = new Vector3(scale, scale, 1f);
    }
}
