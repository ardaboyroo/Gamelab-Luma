using UnityEngine;

/// Sets Luma's frame rate on phones and tablets, once, before the first scene loads.
/// 60 fps feels smooth for touch and the measurements show the game has room for it.
/// Lower it to 30 if phones get warm or the battery drains too fast.
/// In the Editor and on PC nothing is changed.
public static class FrameRateSetup
{
    public const int MobileTargetFps = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        QualitySettings.vSyncCount = 0;                  // on mobile, targetFrameRate decides the rate
        Application.targetFrameRate = MobileTargetFps;
#endif
    }
}
