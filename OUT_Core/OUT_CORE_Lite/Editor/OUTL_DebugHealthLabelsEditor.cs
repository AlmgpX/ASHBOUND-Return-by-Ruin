#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OUTL_DebugHealthLabelsEditor
{
    private const string TogglePath = "OUT CORE Lite/Diagnostics/Runtime Overlays/Toggle Health Labels";
    private const string Mode2Path = "OUT CORE Lite/Diagnostics/Runtime Overlays/Toggle Health Labels Extended";
    private const string OffPath = "OUT CORE Lite/Diagnostics/Runtime Overlays/Disable Health Labels";

    [MenuItem(TogglePath)]
    public static void ToggleHealthLabels()
    {
        OUTL_DebugHealthLabels.Toggle();
        if (OUTL_DebugHealthSettings.Enabled) OUTL_DebugHealthLabels.EnsureInScene();
        Debug.Log("SV_Debug_Health = " + OUTL_DebugHealthSettings.HealthMode + " offset=" + OUTL_DebugHealthSettings.VerticalOffset.ToString("0.00") + " maxDist=" + OUTL_DebugHealthSettings.MaxDistance.ToString("0.0"));
    }

    [MenuItem(Mode2Path)]
    public static void ToggleHealthLabelsExtended()
    {
        OUTL_DebugHealthLabels.SetMode(OUTL_DebugHealthSettings.HealthMode == 2 ? 0 : 2);
        if (OUTL_DebugHealthSettings.Enabled) OUTL_DebugHealthLabels.EnsureInScene();
        Debug.Log("SV_Debug_Health = " + OUTL_DebugHealthSettings.HealthMode + " (extended anchor/source mode)");
    }

    [MenuItem(OffPath)]
    public static void DisableHealthLabels()
    {
        OUTL_DebugHealthLabels.SetMode(0);
        Debug.Log("SV_Debug_Health = 0");
    }
}
#endif
