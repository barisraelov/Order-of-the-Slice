using UnityEngine;

/// <summary>
/// Short-lived swipe ribbon. Presentation only: no collider, no raycasts, no slice logic.
/// </summary>
public class SliceTrailPresenter : MonoBehaviour
{
    private const int MaxPoints = 20;
    private const float MinPointDistance = 0.045f;
    private const float FadeSeconds = 0.16f;

    [SerializeField] private LineRenderer line;
    [SerializeField] private Camera worldCamera;

    private readonly Vector3[] points = new Vector3[MaxPoints];
    private int pointCount;
    private bool drawing;
    private bool fading;
    private float fadeStartUnscaled;
    private Color liveColor = new Color(1f, 0.94f, 0.78f, 0.92f);

    private void Awake()
    {
        if (line == null)
        {
            line = GetComponent<LineRenderer>();
        }

        if (line != null)
        {
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.allowOcclusionWhenDynamic = false;
            line.positionCount = 0;
            line.enabled = false;
        }
    }

    public void Begin(Vector2 screen)
    {
        drawing = true;
        fading = false;
        pointCount = 1;
        points[0] = ToWorld(screen);
        ApplyPoints();
        ApplyAlpha(liveColor.a);
        if (line != null)
        {
            line.enabled = true;
        }
    }

    public void Extend(Vector2 screen)
    {
        if (!drawing || line == null)
        {
            return;
        }

        var world = ToWorld(screen);
        if (pointCount > 0 && (world - points[pointCount - 1]).sqrMagnitude < MinPointDistance * MinPointDistance)
        {
            points[pointCount - 1] = world;
            line.SetPosition(pointCount - 1, world);
            return;
        }

        if (pointCount < MaxPoints)
        {
            points[pointCount] = world;
            pointCount++;
            ApplyPoints();
            return;
        }

        for (var i = 1; i < MaxPoints; i++)
        {
            points[i - 1] = points[i];
        }

        points[MaxPoints - 1] = world;
        line.SetPositions(points);
    }

    public void End()
    {
        if (!drawing)
        {
            return;
        }

        drawing = false;
        if (pointCount <= 0 || line == null || !line.enabled)
        {
            HideImmediate();
            return;
        }

        fading = true;
        fadeStartUnscaled = Time.unscaledTime;
    }

    private void LateUpdate()
    {
        if (!fading)
        {
            return;
        }

        var t = (Time.unscaledTime - fadeStartUnscaled) / FadeSeconds;
        if (t >= 1f)
        {
            HideImmediate();
            return;
        }

        ApplyAlpha(liveColor.a * (1f - t));
    }

    private void HideImmediate()
    {
        drawing = false;
        fading = false;
        pointCount = 0;
        if (line != null)
        {
            line.positionCount = 0;
            line.enabled = false;
        }
    }

    private void ApplyPoints()
    {
        if (line == null)
        {
            return;
        }

        line.positionCount = pointCount;
        for (var i = 0; i < pointCount; i++)
        {
            line.SetPosition(i, points[i]);
        }
    }

    private void ApplyAlpha(float alpha)
    {
        if (line == null)
        {
            return;
        }

        var color = liveColor;
        color.a = alpha;
        line.startColor = color;
        color.a *= 0.35f;
        line.endColor = color;
    }

    private Vector3 ToWorld(Vector2 screen)
    {
        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null)
        {
            return Vector3.zero;
        }

        var z = Mathf.Abs(cam.transform.position.z);
        var world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
        world.z = 0f;
        return world;
    }
}
