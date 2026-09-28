using UnityEngine;

/// <summary>
/// Scales a decorative background sprite to cover the gameplay camera. No collider, no UI,
/// and no physics-root changes.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CameraBackgroundFit : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CameraImpulse cameraImpulse;

    private float lastAspect = -1f;
    private float lastSize = -1f;

    private void LateUpdate()
    {
        FitIfNeeded();
    }

    private void FitIfNeeded()
    {
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        if (Mathf.Approximately(cam.aspect, lastAspect) && Mathf.Approximately(cam.orthographicSize, lastSize))
        {
            return;
        }

        lastAspect = cam.aspect;
        lastSize = cam.orthographicSize;
        Fit(cam);
    }

    private void Fit(Camera cam)
    {
        var spriteSize = spriteRenderer.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        var worldHeight = cam.orthographicSize * 2f;
        var worldWidth = worldHeight * cam.aspect;
        var scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);
        var camPos = cameraImpulse != null ? cameraImpulse.RestWorldPosition : cam.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, 0f);
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
