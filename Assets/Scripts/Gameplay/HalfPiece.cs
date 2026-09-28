using UnityEngine;

/// <summary>
/// Presentation-only food half. No gameplay collider, no Sliceable layer, no slice callback.
/// </summary>
public class HalfPiece : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D body;

    private SlicePresentation owner;
    private float expireAt;

    public void Bind(SlicePresentation presentation)
    {
        owner = presentation;
    }

    public void Launch(Sprite sprite, float worldSize, Vector3 position, Vector2 velocity, float angularVelocity, float lifetime)
    {
        expireAt = Time.time + lifetime;
        transform.SetPositionAndRotation(position, Quaternion.identity);
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            Fit(sprite, worldSize);
        }

        if (body != null)
        {
            body.linearVelocity = velocity;
            body.angularVelocity = angularVelocity;
        }
    }

    public void ResetForPool()
    {
        expireAt = 0f;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private void Update()
    {
        if (expireAt <= 0f || Time.time < expireAt)
        {
            return;
        }

        if (owner != null)
        {
            owner.ReleaseHalf(this);
        }
    }

    private void Fit(Sprite sprite, float worldSize)
    {
        if (sprite == null)
        {
            transform.localScale = Vector3.one;
            return;
        }

        var size = sprite.bounds.size;
        var max = Mathf.Max(size.x, size.y);
        if (max <= 0f)
        {
            return;
        }

        var scale = worldSize / max;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
