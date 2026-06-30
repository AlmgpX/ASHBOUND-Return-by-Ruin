#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

#pragma warning disable 0618

[CustomEditor(typeof(OUTL_ChunkProcessingDriver))]
public sealed class OUTL_ChunkProcessingDriverEditor : Editor
{
    private void OnSceneGUI()
    {
        OUTL_ChunkProcessingDriver driver = (OUTL_ChunkProcessingDriver)target;
        if (driver == null) return;

        Transform focus = driver.Focus;
        Vector3 center = focus != null ? focus.position : driver.transform.position;
        float chunkSize = Mathf.Max(1f, driver.ChunkSize);

        Handles.color = new Color(1f, 0.25f, 0.18f, 0.9f);
        DrawChunkRing(center, chunkSize, driver.FullRadius, "Full");
        Handles.color = new Color(1f, 0.85f, 0.25f, 0.85f);
        DrawChunkRing(center, chunkSize, driver.NearRadius, "Near 3x3");
        Handles.color = new Color(0.35f, 1f, 0.65f, 0.8f);
        DrawChunkRing(center, chunkSize, driver.MidRadius, "Mid");
        Handles.color = new Color(0.3f, 0.5f, 1f, 0.8f);
        DrawChunkRing(center, chunkSize, driver.FarRadius, "Far");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Canonical OUT CORE Lite processing: center chunk = Full, 3x3 = Near, ring 2 = Mid, ring 3 = Far, outside = Dormant. Keep EnforceCanonicalThreeByThree enabled for production.", MessageType.Info);

        OUTL_ChunkProcessingDriver driver = (OUTL_ChunkProcessingDriver)target;
        if (GUILayout.Button("Process All Entities Now"))
        {
            Undo.RecordObject(driver, "Process OUTL Chunk Entities");
            driver.ProcessAllNow();
            SceneView.RepaintAll();
        }
    }

    private static void DrawChunkRing(Vector3 center, float chunkSize, int radius, string label)
    {
        radius = Mathf.Max(0, radius);
        Vector2Int focusChunk = new Vector2Int(Mathf.FloorToInt(center.x / chunkSize), Mathf.FloorToInt(center.z / chunkSize));
        float size = (radius * 2 + 1) * chunkSize;
        Vector3 boxCenter = new Vector3((focusChunk.x + 0.5f) * chunkSize, center.y, (focusChunk.y + 0.5f) * chunkSize);
        Handles.DrawWireCube(boxCenter, new Vector3(size, 0.05f, size));
        Handles.Label(boxCenter + Vector3.forward * (size * 0.5f), "OUTL " + label + " r=" + radius, EditorStyles.boldLabel);
    }
}

[CustomEditor(typeof(OUTL_ProcessingDistanceDriver))]
public sealed class OUTL_ProcessingDistanceDriverEditor : Editor
{
    private void OnSceneGUI()
    {
        OUTL_ProcessingDistanceDriver driver = (OUTL_ProcessingDistanceDriver)target;
        if (driver == null) return;

        OUTL_ProcessingProfile profile = driver.Profile;
        if (profile == null) return;

        Transform focus = driver.Focus;
        Vector3 center = focus != null ? focus.position : driver.transform.position;

        Handles.color = new Color(0.15f, 0.85f, 1f, 0.95f);
        DrawRadius(center, profile.FullDistance, "Full " + profile.FullDistance.ToString("0.#"));
        Handles.color = new Color(0.2f, 1f, 0.35f, 0.85f);
        DrawRadius(center, profile.NearDistance, "Near " + profile.NearDistance.ToString("0.#"));
        Handles.color = new Color(1f, 0.8f, 0.2f, 0.75f);
        DrawRadius(center, profile.MidDistance, "Mid " + profile.MidDistance.ToString("0.#"));
        Handles.color = new Color(1f, 0.35f, 0.15f, 0.65f);
        DrawRadius(center, profile.FarDistance, "Far " + profile.FarDistance.ToString("0.#"));

        if (profile.ApplySectorCellSize)
            DrawSectorGrid(center, profile.SectorCellSize, profile.FarDistance);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Legacy fallback only. Canonical OUT CORE Lite scenes should use OUTL_ChunkProcessingDriver.", MessageType.Warning);

        OUTL_ProcessingDistanceDriver driver = (OUTL_ProcessingDistanceDriver)target;
        if (GUILayout.Button("Process All Entities Now"))
        {
            Undo.RecordObject(driver, "Process OUTL Entities");
            driver.ProcessAllNow();
            SceneView.RepaintAll();
        }
    }

    private static void DrawRadius(Vector3 center, float radius, string label)
    {
        if (radius <= 0f) return;
        Handles.DrawWireDisc(center, Vector3.up, radius);
        Handles.Label(center + Vector3.forward * radius, "OUTL " + label, EditorStyles.boldLabel);
    }

    private static void DrawSectorGrid(Vector3 center, float cellSize, float radius)
    {
        if (cellSize <= 0f || radius <= 0f) return;
        int cells = Mathf.CeilToInt(radius / cellSize);
        Vector3 origin = new Vector3(Mathf.Floor(center.x / cellSize) * cellSize, center.y, Mathf.Floor(center.z / cellSize) * cellSize);
        Color old = Handles.color;
        Handles.color = new Color(0.45f, 0.45f, 0.45f, 0.18f);
        for (int x = -cells; x <= cells; x++)
        {
            float gx = origin.x + x * cellSize;
            Handles.DrawLine(new Vector3(gx, center.y, origin.z - cells * cellSize), new Vector3(gx, center.y, origin.z + cells * cellSize));
        }
        for (int z = -cells; z <= cells; z++)
        {
            float gz = origin.z + z * cellSize;
            Handles.DrawLine(new Vector3(origin.x - cells * cellSize, center.y, gz), new Vector3(origin.x + cells * cellSize, center.y, gz));
        }
        Handles.color = old;
    }
}
#endif
