using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Pooled slice score popups. Does not calculate score.
/// </summary>
public class ScoreFeedback : MonoBehaviour
{
    [SerializeField] private ScorePopup popupPrefab;
    [SerializeField] private int popupPrewarm = 8;
    [SerializeField] private int popupMax = 16;
    [SerializeField] private float popupLifetime = 0.72f;

    private static readonly Color IngredientColor = new Color(1f, 0.95f, 0.72f, 1f);
    private static readonly Color FeverColor = new Color(1f, 0.72f, 0.28f, 1f);
    private static readonly Color CompleteColor = new Color(0.72f, 1f, 0.62f, 1f);

    private ObjectPool<ScorePopup> pool;
    private Transform root;
    private readonly HashSet<ScorePopup> active = new HashSet<ScorePopup>();
    private readonly List<ScorePopup> buffer = new List<ScorePopup>(16);

    private void Awake()
    {
        root = new GameObject("ScorePopupPool").transform;
        root.SetParent(transform, false);
        pool = new ObjectPool<ScorePopup>(
            createFunc: CreatePopup,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: null,
            collectionCheck: true,
            defaultCapacity: popupPrewarm,
            maxSize: popupMax);
    }

    private void Start()
    {
        buffer.Clear();
        for (var i = 0; i < popupPrewarm; i++)
        {
            buffer.Add(pool.Get());
        }

        for (var i = 0; i < buffer.Count; i++)
        {
            pool.Release(buffer[i]);
        }

        buffer.Clear();
    }

    public void ShowIngredient(Vector3 position, int points, int multiplier, bool fever)
    {
        if (points <= 0)
        {
            return;
        }

        var text = multiplier > 1 ? "+" + points + " x" + multiplier : "+" + points;
        Spawn(text, fever ? FeverColor : IngredientColor, position + new Vector3(0f, 0.35f, 0f));
    }

    public void ShowCompletion(Vector3 position, int bonus)
    {
        if (bonus <= 0)
        {
            return;
        }

        Spawn("+" + bonus, CompleteColor, position + new Vector3(0f, 0.95f, 0f));
    }

    public void ReleaseAll()
    {
        buffer.Clear();
        buffer.AddRange(active);
        for (var i = 0; i < buffer.Count; i++)
        {
            ReleasePopup(buffer[i]);
        }

        buffer.Clear();
    }

    public void ReleasePopup(ScorePopup popup)
    {
        if (popup == null || !popup.isActiveAndEnabled)
        {
            return;
        }

        pool.Release(popup);
    }

    private void Spawn(string text, Color color, Vector3 position)
    {
        var popup = pool.Get();
        popup.Play(text, color, position, popupLifetime);
    }

    private ScorePopup CreatePopup()
    {
        var instance = Instantiate(popupPrefab, root);
        instance.Bind(this);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void OnGet(ScorePopup popup)
    {
        popup.ResetForPool();
        popup.gameObject.SetActive(true);
        active.Add(popup);
    }

    private void OnRelease(ScorePopup popup)
    {
        active.Remove(popup);
        popup.ResetForPool();
        popup.gameObject.SetActive(false);
    }
}
