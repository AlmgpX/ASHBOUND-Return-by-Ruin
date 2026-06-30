using System.Collections;
using UnityEngine;

public static class OUT_SimTime
{
    public static bool HasSimulationService => OUT_SimulationService.Instance != null;

    public static float Time
    {
        get
        {
            OUT_SimulationService service = OUT_SimulationService.Instance;
            return service != null ? service.RuntimeTime : UnityEngine.Time.time;
        }
    }

    public static float DeltaTime
    {
        get
        {
            OUT_SimulationService service = OUT_SimulationService.Instance;
            return service != null ? service.RuntimeDeltaTime : UnityEngine.Time.deltaTime;
        }
    }

    public static float UnscaledDeltaTime
    {
        get
        {
            OUT_SimulationService service = OUT_SimulationService.Instance;
            return service != null ? service.RuntimeUnscaledDeltaTime : UnityEngine.Time.unscaledDeltaTime;
        }
    }

    public static float TimeScale
    {
        get
        {
            OUT_SimulationService service = OUT_SimulationService.Instance;
            return service != null ? service.SimulationTimeScale : UnityEngine.Time.timeScale;
        }
    }

    public static bool IsPaused
    {
        get
        {
            OUT_SimulationService service = OUT_SimulationService.Instance;
            return service != null && service.Paused && service.RuntimeDeltaTime <= 0f;
        }
    }

    public static int FrameCount => UnityEngine.Time.frameCount;

    // Unity-style aliases. Add `using Time = OUT_SimTime;` in old scripts and most timing code follows OUT simulation time.
    public static float time => Time;
    public static float deltaTime => DeltaTime;
    public static float unscaledDeltaTime => UnscaledDeltaTime;
    public static float timeScale => TimeScale;
    public static int frameCount => FrameCount;

    public static IEnumerator WaitForSeconds(float seconds)
    {
        float remaining = Mathf.Max(0f, seconds);
        while (remaining > 0f)
        {
            yield return null;
            remaining -= DeltaTime;
        }
    }
}
