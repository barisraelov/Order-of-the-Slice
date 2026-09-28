using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Pooled slice juice: halves, splat, camera shake. Does not score, spawn gameplay
/// objects, or change recipe/life state.
/// </summary>
public class SlicePresentation : MonoBehaviour
{
    [SerializeField] private IngredientVisualCatalog catalog;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CameraImpulse cameraImpulse;
    [SerializeField] private HalfPiece halfPrefab;
    [SerializeField] private SplatBurst splatPrefab;
    [SerializeField] private int halfPrewarm = 16;
    [SerializeField] private int halfMax = 32;
    [SerializeField] private int splatPrewarm = 8;
    [SerializeField] private int splatMax = 16;
    [SerializeField] private float halfLifetime = 0.62f;
    [SerializeField] private float splatLifetime = 0.38f;

    private ObjectPool<HalfPiece> halfPool;
    private ObjectPool<SplatBurst> splatPool;
    private Transform halfRoot;
    private Transform splatRoot;
    private readonly HashSet<HalfPiece> activeHalves = new HashSet<HalfPiece>();
    private readonly HashSet<SplatBurst> activeSplats = new HashSet<SplatBurst>();
    private readonly List<HalfPiece> halfBuffer = new List<HalfPiece>(32);
    private readonly List<SplatBurst> splatBuffer = new List<SplatBurst>(16);
    private static readonly Color BombBurst = new Color(0.95f, 0.32f, 0.12f, 1f);

    private void Awake()
    {
        halfRoot = new GameObject("HalfPool").transform;
        halfRoot.SetParent(transform, false);
        splatRoot = new GameObject("SplatPool").transform;
        splatRoot.SetParent(transform, false);

        halfPool = new ObjectPool<HalfPiece>(
            createFunc: CreateHalf,
            actionOnGet: OnGetHalf,
            actionOnRelease: OnReleaseHalf,
            actionOnDestroy: null,
            collectionCheck: true,
            defaultCapacity: halfPrewarm,
            maxSize: halfMax);

        splatPool = new ObjectPool<SplatBurst>(
            createFunc: CreateSplat,
            actionOnGet: OnGetSplat,
            actionOnRelease: OnReleaseSplat,
            actionOnDestroy: null,
            collectionCheck: true,
            defaultCapacity: splatPrewarm,
            maxSize: splatMax);
    }

    private void Start()
    {
        PrewarmHalves();
        PrewarmSplats();
    }

    public void Play(Vector3 position, IngredientId id, bool isBomb, bool isCorrect)
    {
        if (isBomb)
        {
            SpawnSplat(position, BombBurst);
            if (cameraImpulse != null)
            {
                cameraImpulse.Play(0.16f, 0.16f);
            }

            if (gameManager != null)
            {
                gameManager.RequestHitStop(0.055f, 0.12f);
            }

            return;
        }

        SpawnHalves(position, id);
        SpawnSplat(position, catalog != null ? catalog.GetEffectTint(id) : Color.white);
        if (cameraImpulse != null)
        {
            cameraImpulse.Play(isCorrect ? 0.035f : 0.075f, isCorrect ? 0.07f : 0.10f);
        }
    }

    public void ReleaseAll()
    {
        halfBuffer.Clear();
        halfBuffer.AddRange(activeHalves);
        for (var i = 0; i < halfBuffer.Count; i++)
        {
            ReleaseHalf(halfBuffer[i]);
        }

        halfBuffer.Clear();
        splatBuffer.Clear();
        splatBuffer.AddRange(activeSplats);
        for (var i = 0; i < splatBuffer.Count; i++)
        {
            ReleaseSplat(splatBuffer[i]);
        }

        splatBuffer.Clear();
        if (cameraImpulse != null)
        {
            cameraImpulse.StopImmediate();
        }
    }

    public void ReleaseHalf(HalfPiece piece)
    {
        if (piece == null || !piece.isActiveAndEnabled)
        {
            return;
        }

        halfPool.Release(piece);
    }

    public void ReleaseSplat(SplatBurst splat)
    {
        if (splat == null || !splat.isActiveAndEnabled)
        {
            return;
        }

        splatPool.Release(splat);
    }

    private void SpawnHalves(Vector3 position, IngredientId id)
    {
        if (catalog == null || !catalog.TryGetHalves(id, out var halfA, out var halfB))
        {
            return;
        }

        var size = catalog.VisualWorldSize * 0.92f;
        LaunchHalf(halfA, position, new Vector2(-1.85f, 1.35f), 220f, size);
        LaunchHalf(halfB, position, new Vector2(1.85f, 1.35f), -220f, size);
    }

    private void LaunchHalf(Sprite sprite, Vector3 position, Vector2 velocity, float angular, float size)
    {
        var piece = halfPool.Get();
        piece.Launch(sprite, size, position, velocity, angular, halfLifetime);
    }

    private void SpawnSplat(Vector3 position, Color tint)
    {
        var splat = splatPool.Get();
        splat.Play(position, tint, splatLifetime);
    }

    private HalfPiece CreateHalf()
    {
        var instance = Instantiate(halfPrefab, halfRoot);
        instance.Bind(this);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private SplatBurst CreateSplat()
    {
        var instance = Instantiate(splatPrefab, splatRoot);
        instance.Bind(this);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void OnGetHalf(HalfPiece piece)
    {
        piece.ResetForPool();
        piece.gameObject.SetActive(true);
        activeHalves.Add(piece);
    }

    private void OnReleaseHalf(HalfPiece piece)
    {
        activeHalves.Remove(piece);
        piece.ResetForPool();
        piece.gameObject.SetActive(false);
    }

    private void OnGetSplat(SplatBurst splat)
    {
        splat.ResetForPool();
        splat.gameObject.SetActive(true);
        activeSplats.Add(splat);
    }

    private void OnReleaseSplat(SplatBurst splat)
    {
        activeSplats.Remove(splat);
        splat.ResetForPool();
        splat.gameObject.SetActive(false);
    }

    private void PrewarmHalves()
    {
        halfBuffer.Clear();
        for (var i = 0; i < halfPrewarm; i++)
        {
            halfBuffer.Add(halfPool.Get());
        }

        for (var i = 0; i < halfBuffer.Count; i++)
        {
            halfPool.Release(halfBuffer[i]);
        }

        halfBuffer.Clear();
    }

    private void PrewarmSplats()
    {
        splatBuffer.Clear();
        for (var i = 0; i < splatPrewarm; i++)
        {
            splatBuffer.Add(splatPool.Get());
        }

        for (var i = 0; i < splatBuffer.Count; i++)
        {
            splatPool.Release(splatBuffer[i]);
        }

        splatBuffer.Clear();
    }
}
