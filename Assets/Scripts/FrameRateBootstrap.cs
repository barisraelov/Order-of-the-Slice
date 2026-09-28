using UnityEngine;

/// <summary>
/// Sets vSync off and a 60 FPS target before the first scene loads.
/// targetFrameRate is not a Player Setting, so it is applied here.
/// </summary>
public static class FrameRateBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
}
