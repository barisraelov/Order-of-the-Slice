using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Typed pools for ingredients and bombs.
/// Get/release/reset pattern adapted from course SimpleAsteroids BulletManager, without a singleton.
/// </summary>
[DefaultExecutionOrder(-200)]
public class PoolManager : MonoBehaviour
{
    [SerializeField] private SliceableObject ingredientPrefab;
    [SerializeField] private SliceableObject bombPrefab;
    [SerializeField] private GameConfig config;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RecipeManager recipeManager;
    [SerializeField] private SpawnDirector spawnDirector;

    private ObjectPool<SliceableObject> ingredientPool;
    private ObjectPool<SliceableObject> bombPool;
    private Transform ingredientRoot;
    private Transform bombRoot;
    private int ingredientCreates;
    private int bombCreates;

    // Single active-object set, updated only from the pool's own OnGet/OnRelease callbacks so
    // membership can never drift even if a future path bypasses GetIngredient/GetBomb/Release.
    // A single set is used (not one per pool) because Release already branches on IsBomb, so a
    // second set would only double the drift surface for no benefit.
    private readonly HashSet<SliceableObject> active = new HashSet<SliceableObject>();
    private readonly List<SliceableObject> releaseBuffer = new List<SliceableObject>(32);

    public int IngredientCreateCount => ingredientCreates;
    public int BombCreateCount => bombCreates;
    public int ActiveCount => active.Count;

    private void Awake()
    {
        ingredientRoot = new GameObject("IngredientPool").transform;
        ingredientRoot.SetParent(transform, false);
        bombRoot = new GameObject("BombPool").transform;
        bombRoot.SetParent(transform, false);

        ingredientPool = new ObjectPool<SliceableObject>(
            createFunc: () => Create(ingredientPrefab, ingredientRoot, ref ingredientCreates),
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: null,
            collectionCheck: true,
            defaultCapacity: config.ingredientPrewarm,
            maxSize: config.ingredientMax);

        bombPool = new ObjectPool<SliceableObject>(
            createFunc: () => Create(bombPrefab, bombRoot, ref bombCreates),
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: null,
            collectionCheck: true,
            defaultCapacity: config.bombPrewarm,
            maxSize: config.bombMax);
    }

    private void Start()
    {
        Prewarm(ingredientPool, config.ingredientPrewarm);
        Prewarm(bombPool, config.bombPrewarm);
    }

    public SliceableObject GetIngredient(IngredientId id)
    {
        var item = ingredientPool.Get();
        item.ConfigureIngredient(id);
        return item;
    }

    public SliceableObject GetBomb()
    {
        var item = bombPool.Get();
        item.ConfigureBomb();
        return item;
    }

    public void Release(SliceableObject item)
    {
        if (item == null || !item.isActiveAndEnabled)
        {
            return;
        }

        if (item.IsBomb)
        {
            bombPool.Release(item);
        }
        else
        {
            ingredientPool.Release(item);
        }
    }

    private SliceableObject Create(SliceableObject prefab, Transform parent, ref int counter)
    {
        counter++;
        var instance = Instantiate(prefab, parent);
        instance.Bind(gameManager, recipeManager, this, spawnDirector, config);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void OnGet(SliceableObject item)
    {
        active.Add(item);
        item.ResetForPool();
        item.gameObject.SetActive(true);
    }

    private void OnRelease(SliceableObject item)
    {
        active.Remove(item);
        item.ResetForPool();
        item.gameObject.SetActive(false);
    }

    /// <summary>
    /// Releases every currently active ingredient and bomb. Iterates a copied buffer because
    /// Release() mutates the "active" set while we would otherwise be enumerating it. Safe to
    /// call when already empty (Game Over followed by Restart calls this twice) and safe against
    /// double-release: Release() early-returns on !isActiveAndEnabled, and ObjectPool&lt;T&gt; itself
    /// was constructed with collectionCheck: true, which throws on a duplicate release. No
    /// scene-wide FindObjectsByType search is used.
    /// </summary>
    public void ReleaseAll()
    {
        releaseBuffer.Clear();
        releaseBuffer.AddRange(active);
        for (var i = 0; i < releaseBuffer.Count; i++)
        {
            Release(releaseBuffer[i]);
        }

        releaseBuffer.Clear();
        active.Clear(); // defensive; Release should already have emptied it via OnRelease
    }

    private static void Prewarm(ObjectPool<SliceableObject> pool, int count)
    {
        var buffer = new List<SliceableObject>(count);
        for (var i = 0; i < count; i++)
        {
            buffer.Add(pool.Get());
        }

        for (var i = 0; i < buffer.Count; i++)
        {
            pool.Release(buffer[i]);
        }
    }
}
