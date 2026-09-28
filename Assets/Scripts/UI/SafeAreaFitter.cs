using UnityEngine;

/// <summary>
/// Stretches a RectTransform to the current Screen.safeArea. Applied to HUD and Main Menu
/// content roots so 16:9 and 20:9 landscape stay readable.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastSafeArea;
    private Vector2Int lastScreen;

    private void Awake()
    {
        rect = (RectTransform)transform;
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreen.x || Screen.height != lastScreen.y)
        {
            Apply();
        }
    }

    public void Apply()
    {
        if (rect == null)
        {
            rect = (RectTransform)transform;
        }

        lastSafeArea = Screen.safeArea;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width < 1 || Screen.height < 1 || lastSafeArea.width < 1f || lastSafeArea.height < 1f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return;
        }

        var safe = lastSafeArea;
        rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
