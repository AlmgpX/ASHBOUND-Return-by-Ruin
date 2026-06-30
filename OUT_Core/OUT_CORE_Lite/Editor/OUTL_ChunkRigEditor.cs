#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class OUTL_ChunkRigEditor
{
    // [MenuItem("OUT CORE Lite/Debug/Create Chunk Processing Rig")]
    public static void CreateChunkProcessingRig()
    {
        GameObject runtime = GameObject.Find("OUTL_Runtime");
        if (runtime == null)
        {
            runtime = new GameObject("OUTL_Runtime");
            Undo.RegisterCreatedObjectUndo(runtime, "Create OUTL Runtime");
        }
        if (runtime.GetComponent<OUTL_World>() == null) runtime.AddComponent<OUTL_World>();

        OUTL_ChunkProcessingDriver driver = runtime.GetComponent<OUTL_ChunkProcessingDriver>();
        if (driver == null) driver = runtime.AddComponent<OUTL_ChunkProcessingDriver>();
        driver.BuiltInPreset = OUTL_ProcessingBuiltInPreset.StreamingWorld;
        driver.EnforceCanonicalThreeByThree = true;
        driver.ChunkSize = 64f;
        driver.FullRadius = 0;
        driver.NearRadius = 1;
        driver.MidRadius = 2;
        driver.FarRadius = 3;

        OUTL_ChunkDebugView debug = runtime.GetComponent<OUTL_ChunkDebugView>();
        if (debug == null) debug = runtime.AddComponent<OUTL_ChunkDebugView>();
        debug.Driver = driver;
        debug.ChunkSize = driver.ChunkSize;
        debug.ViewRadius = 8;

        OUTL_GoldenTestRunner golden = runtime.GetComponent<OUTL_GoldenTestRunner>();
        if (golden == null) golden = runtime.AddComponent<OUTL_GoldenTestRunner>();
        golden.ChunkDriver = driver;

        EditorUtility.SetDirty(runtime);
        Selection.activeGameObject = runtime;
        EditorGUIUtility.PingObject(runtime);
        Debug.Log("OUTL Chunk Processing Rig created. Press Play, F2/F3 for overlay/gizmos, use context menu OUT Run Golden Tests.");
    }
}
#endif
