using UnityEngine;

/// <summary>
/// Short unscaled camera shake that always restores the exact captured rest pose.
/// Background fitting should read RestWorldPosition, not the shaken transform.
/// </summary>
[DefaultExecutionOrder(50)]
public class CameraImpulse : MonoBehaviour
{
    private Vector3 restLocal;
    private bool restCaptured;
    private bool shaking;
    private float amplitude;
    private float duration;
    private float startedUnscaled;

    public Vector3 RestWorldPosition
    {
        get
        {
            EnsureRest();
            var parent = transform.parent;
            return parent != null ? parent.TransformPoint(restLocal) : restLocal;
        }
    }

    private void Awake()
    {
        EnsureRest();
    }

    public void Play(float shakeAmplitude, float shakeDuration)
    {
        EnsureRest();
        amplitude = shakeAmplitude;
        duration = Mathf.Max(0.01f, shakeDuration);
        startedUnscaled = Time.unscaledTime;
        shaking = true;
    }

    public void StopImmediate()
    {
        if (!restCaptured)
        {
            return;
        }

        shaking = false;
        transform.localPosition = restLocal;
    }

    private void LateUpdate()
    {
        if (!shaking)
        {
            return;
        }

        var elapsed = Time.unscaledTime - startedUnscaled;
        if (elapsed >= duration)
        {
            transform.localPosition = restLocal;
            shaking = false;
            return;
        }

        var fade = 1f - (elapsed / duration);
        fade *= fade;
        var ox = (Mathf.PerlinNoise(elapsed * 42f, 0.17f) - 0.5f) * 2f * amplitude * fade;
        var oy = (Mathf.PerlinNoise(2.11f, elapsed * 42f) - 0.5f) * 2f * amplitude * fade;
        transform.localPosition = restLocal + new Vector3(ox, oy, 0f);
    }

    private void EnsureRest()
    {
        if (restCaptured)
        {
            return;
        }

        restLocal = transform.localPosition;
        restCaptured = true;
    }
}
