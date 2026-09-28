using UnityEngine;

/// <summary>
/// Pooled tinted burst. Uses a prefab ParticleSystem; never creates materials at runtime.
/// </summary>
public class SplatBurst : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;

    private SlicePresentation owner;
    private float expireAtUnscaled;

    public void Bind(SlicePresentation presentation)
    {
        owner = presentation;
        if (particles == null)
        {
            particles = GetComponent<ParticleSystem>();
        }
    }

    public void Play(Vector3 position, Color tint, float lifetime)
    {
        transform.position = position;
        expireAtUnscaled = Time.unscaledTime + lifetime;
        if (particles == null)
        {
            return;
        }

        var main = particles.main;
        main.startColor = tint;
        particles.Clear(true);
        particles.Play(true);
    }

    public void ResetForPool()
    {
        expireAtUnscaled = 0f;
        if (particles != null)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Update()
    {
        if (expireAtUnscaled <= 0f || Time.unscaledTime < expireAtUnscaled)
        {
            return;
        }

        if (owner != null)
        {
            owner.ReleaseSplat(this);
        }
    }
}
