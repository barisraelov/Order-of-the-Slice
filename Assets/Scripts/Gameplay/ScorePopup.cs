using TMPro;
using UnityEngine;

/// <summary>
/// Pooled floating score label. Presentation only.
/// </summary>
public class ScorePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private ScoreFeedback owner;
    private float expireAt;
    private float lifetime;
    private Vector3 velocity;
    private Color baseColor;

    public void Bind(ScoreFeedback feedback)
    {
        owner = feedback;
        if (label == null)
        {
            label = GetComponent<TMP_Text>();
        }
    }

    public void Play(string text, Color color, Vector3 position, float duration)
    {
        transform.position = position;
        lifetime = duration;
        expireAt = Time.time + duration;
        velocity = new Vector3(0f, 1.55f, 0f);
        baseColor = color;
        if (label != null)
        {
            label.text = text;
            label.color = color;
            label.raycastTarget = false;
        }
    }

    public void ResetForPool()
    {
        expireAt = 0f;
        if (label != null)
        {
            label.text = string.Empty;
        }
    }

    private void Update()
    {
        if (expireAt <= 0f)
        {
            return;
        }

        transform.position += velocity * Time.deltaTime;
        var remaining = expireAt - Time.time;
        if (label != null && lifetime > 0f)
        {
            var color = baseColor;
            color.a = Mathf.Clamp01(remaining / lifetime);
            label.color = color;
        }

        if (remaining > 0f)
        {
            return;
        }

        if (owner != null)
        {
            owner.ReleasePopup(this);
        }
    }
}
