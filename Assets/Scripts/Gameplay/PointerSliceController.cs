using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Shared Pointer slice: dual-axis reference speed, Linecast-all, UI RaycastAll at press start.
/// A swipe must pass a minimum speed. The detector is Physics2D.Linecast, not a moving collider.
/// </summary>
public class PointerSliceController : MonoBehaviour
{
    private const float MinDeltaTime = 0.00001f;

    [SerializeField] private GameConfig config;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask sliceableMask;
    [SerializeField] private SliceTrailPresenter trail;

    private readonly List<RaycastHit2D> lineHits = new List<RaycastHit2D>(16);
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>(16);
    private readonly HashSet<SliceableObject> uniqueHits = new HashSet<SliceableObject>();
    private PointerEventData pointerEvent;

    private ContactFilter2D filter;
    private bool ignoreUntilRelease;
    private bool hasPrev;
    private Vector2 prevScreen;

    public bool HasPreviousPoint => hasPrev;

    private void Awake()
    {
        filter = new ContactFilter2D();
        filter.SetLayerMask(sliceableMask);
        filter.useLayerMask = true;
        filter.useTriggers = false;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            ClearSwipe();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            ClearSwipe();
        }
    }

    private void Update()
    {
        // Slicing is allowed only while Playing or Fever. Pause, Countdown, Game Over,
        // and the menu exit all clear the swipe.
        if (gameManager == null || !gameManager.IsGameplayActive)
        {
            ClearSwipe();
            return;
        }

        var pointer = Pointer.current;
        if (pointer == null)
        {
            return;
        }

        ProcessPointer(
            pointer.position.ReadValue(),
            pointer.press.wasPressedThisFrame,
            pointer.press.wasReleasedThisFrame,
            pointer.press.isPressed);
    }

    /// <summary>
    /// Press stores a valid previous point so the next held or release frame can slice.
    /// Fast press-drag-release is two frames: press, then movement+release.
    /// </summary>
    public void ProcessPointer(Vector2 screen, bool pressedThisFrame, bool releasedThisFrame, bool isPressed)
    {
        if (pressedThisFrame)
        {
            ignoreUntilRelease = IsPointerOverRaycastableUi(screen);
            prevScreen = screen;
            hasPrev = !ignoreUntilRelease;
            if (hasPrev && trail != null)
            {
                trail.Begin(screen);
            }

            if (releasedThisFrame)
            {
                TrySliceIfActive(screen);
                ClearSwipe();
            }

            return;
        }

        if (ignoreUntilRelease)
        {
            if (releasedThisFrame)
            {
                ClearSwipe();
            }

            return;
        }

        if ((isPressed || releasedThisFrame) && hasPrev)
        {
            TrySliceIfActive(screen);
            if (trail != null)
            {
                trail.Extend(screen);
            }

            prevScreen = screen;
        }

        if (releasedThisFrame)
        {
            ClearSwipe();
        }
    }

    private void TrySliceIfActive(Vector2 toScreen)
    {
        if (!hasPrev)
        {
            return;
        }

        TrySliceSegment(prevScreen, toScreen);
    }

    private void TrySliceSegment(Vector2 fromScreen, Vector2 toScreen)
    {
        var dt = Time.unscaledDeltaTime;
        if (dt < MinDeltaTime || Screen.width < 1 || Screen.height < 1)
        {
            return;
        }

        var dx = toScreen.x - fromScreen.x;
        var dy = toScreen.y - fromScreen.y;
        var dxRef = dx * config.referenceWidth / Screen.width;
        var dyRef = dy * config.referenceHeight / Screen.height;
        var speed = Mathf.Sqrt(dxRef * dxRef + dyRef * dyRef) / dt;
        if (speed < config.minimumSliceSpeed)
        {
            return;
        }

        var cam = worldCamera != null ? worldCamera : Camera.main;
        var z = Mathf.Abs(cam.transform.position.z);
        Vector2 fromWorld = cam.ScreenToWorldPoint(new Vector3(fromScreen.x, fromScreen.y, z));
        Vector2 toWorld = cam.ScreenToWorldPoint(new Vector3(toScreen.x, toScreen.y, z));

        lineHits.Clear();
        Physics2D.Linecast(fromWorld, toWorld, filter, lineHits);
        uniqueHits.Clear();
        for (var i = 0; i < lineHits.Count; i++)
        {
            var sliceable = lineHits[i].collider != null
                ? lineHits[i].collider.GetComponent<SliceableObject>()
                : null;
            if (sliceable != null)
            {
                uniqueHits.Add(sliceable);
            }
        }

        foreach (var sliceable in uniqueHits)
        {
            sliceable.TryAcceptSlice();
        }
    }

    private bool IsPointerOverRaycastableUi(Vector2 screenPosition)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        uiHits.Clear();
        if (pointerEvent == null)
        {
            pointerEvent = new PointerEventData(eventSystem);
        }

        pointerEvent.Reset();
        pointerEvent.position = screenPosition;
        eventSystem.RaycastAll(pointerEvent, uiHits);
        return uiHits.Count > 0;
    }

    /// <summary>
    /// Clears any in-progress swipe. Called reactively every frame slicing is not allowed, and
    /// also called directly and synchronously by GameManager when entering Paused, so a live
    /// swipe cannot survive into the frame after Resume regardless of script execution order.
    /// </summary>
    public void ClearSwipe()
    {
        ignoreUntilRelease = false;
        hasPrev = false;
        if (trail != null)
        {
            trail.End();
        }
    }
}
