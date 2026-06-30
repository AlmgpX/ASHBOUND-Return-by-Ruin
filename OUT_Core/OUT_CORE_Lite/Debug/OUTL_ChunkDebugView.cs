using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class OUTL_ChunkDebugView : MonoBehaviour
{
    public OUTL_ChunkProcessingDriver Driver;
    public Transform Focus;
    [Tooltip("Legacy compatibility field. Runtime scene camera lookup is intentionally not used by this debug view.")]
    public bool UseMainCameraAsFocus = false;
    [Tooltip("Legacy compatibility field. Runtime Unity tag lookup is intentionally not used by this debug view.")]
    public bool UsePlayerTagAsFallback = false;
    public bool UseRegistryFocusFallback = true;
    public string FocusTargetName = "player";
    public string FocusClassName = "player";
    public bool DrawInEditMode = true;
    public bool DrawOnlyWhenSelected;

    [Header("Grid")]
    public bool DrawGrid = true;
    public bool DrawActiveChunks = true;
    public bool DrawEntities = true;
    public bool DrawLabels = true;
    public int ViewRadius = 8;
    public float ChunkSize = 64f;
    public float YOffset = 0.05f;

    [Header("Overlay")]
    public bool ShowOverlay = true;
    public KeyCode ToggleOverlayKey = KeyCode.F2;
    public KeyCode ToggleGizmosKey = KeyCode.F3;
    public Rect OverlayRect = new Rect(12, 12, 420, 360);

    [Header("Colors")]
    public Color GridColor = new Color(0f, 0.55f, 1f, 0.18f);
    public Color FocusColor = new Color(1f, 0.92f, 0.25f, 0.55f);
    public Color FullColor = new Color(1f, 0.2f, 0.16f, 0.75f);
    public Color NearColor = new Color(1f, 0.82f, 0.2f, 0.65f);
    public Color MidColor = new Color(0.2f, 1f, 0.55f, 0.52f);
    public Color FarColor = new Color(0.25f, 0.45f, 1f, 0.45f);
    public Color DormantColor = new Color(0.25f, 0.25f, 0.25f, 0.35f);

    private readonly List<OUTL_EntityRuntime> entities = new List<OUTL_EntityRuntime>(1024);
    private readonly Dictionary<long, ChunkInfo> chunks = new Dictionary<long, ChunkInfo>(256);
    private bool overlayVisible = true;
    private bool gizmosVisible = true;

    private struct ChunkInfo { public int Count; public OUTL_RuntimeTier MaxTier; }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (Input.GetKeyDown(ToggleOverlayKey)) overlayVisible = !overlayVisible;
        if (Input.GetKeyDown(ToggleGizmosKey)) gizmosVisible = !gizmosVisible;
    }

    private void OnDrawGizmos()
    {
        if (DrawOnlyWhenSelected) return;
        Draw(false);
    }

    private void OnDrawGizmosSelected()
    {
        Draw(true);
    }

    private void OnGUI()
    {
        if (!ShowOverlay || !overlayVisible) return;
        OUTL_World world = OUTL_World.Instance;
        GUILayout.BeginArea(OverlayRect, GUI.skin.box);
        GUILayout.Label("OUTL Chunk / Processing Debug");
        GUILayout.Label(world != null ? ("world t=" + world.WorldTime.ToString("0.00") + " ent=" + world.Registry.Count + " paused=" + world.IsPaused) : "NO OUTL_WORLD");
        ResolveRefs();
        Vector2Int fc = Focus != null ? OUTL_ChunkProcessingDriver.WorldToChunk(Focus.position, GetChunkSize()) : Vector2Int.zero;
        GUILayout.Label("focus chunk: " + fc);
        GUILayout.Label("chunks visible: " + chunks.Count + " entities sampled: " + entities.Count);
        if (Driver != null)
        {
            GUILayout.Label("driver preset: " + Driver.BuiltInPreset + " size=" + Driver.ChunkSize);
            GUILayout.Label("rings F/N/M/Far: " + Driver.FullRadius + "/" + Driver.NearRadius + "/" + Driver.MidRadius + "/" + Driver.FarRadius);
            GUILayout.Label("parallel rows: snapshot=" + Driver.ParallelSnapshotRowCount + " tier=" + Driver.ParallelTierResultCount + " changed=" + Driver.ParallelTierChangeCount + " aiDebug=" + Driver.ParallelAIDebugRowCount);
        }
        DrawTierStats();
        GUILayout.Space(4);
        GUILayout.Label("F2 overlay, F3 gizmos");
        GUILayout.EndArea();
    }

    private void Draw(bool selected)
    {
        if (!gizmosVisible) return;
        if (!DrawInEditMode && !Application.isPlaying) return;
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        ResolveRefs();
        if (Focus == null) return;
        float size = GetChunkSize();
        Vector2Int focusChunk = OUTL_ChunkProcessingDriver.WorldToChunk(Focus.position, size);
        BuildChunkMap(world, size);
        if (DrawGrid) DrawGridCells(focusChunk, size);
        if (DrawActiveChunks) DrawChunks(size);
        if (DrawEntities) DrawEntityMarkers();
    }

    private void BuildChunkMap(OUTL_World world, float size)
    {
        chunks.Clear();
        entities.Clear();
        world.Registry.CopyAll(entities);
        for (int i = 0; i < entities.Count; i++)
        {
            OUTL_EntityRuntime e = entities[i];
            if (e == null || e.Adapter == null) continue;
            Vector2Int c = OUTL_ChunkProcessingDriver.WorldToChunk(e.Adapter.transform.position, size);
            long key = Key(c.x, c.y);
            ChunkInfo info;
            if (!chunks.TryGetValue(key, out info)) info = new ChunkInfo { Count = 0, MaxTier = OUTL_RuntimeTier.Dormant };
            info.Count++;
            if ((int)e.Tier > (int)info.MaxTier) info.MaxTier = e.Tier;
            chunks[key] = info;
        }
    }

    private void DrawGridCells(Vector2Int focus, float size)
    {
        for (int z = -ViewRadius; z <= ViewRadius; z++)
        for (int x = -ViewRadius; x <= ViewRadius; x++)
        {
            Vector2Int c = new Vector2Int(focus.x + x, focus.y + z);
            DrawCell(c, size, c == focus ? FocusColor : GridColor, false);
        }
    }

    private void DrawChunks(float size)
    {
        foreach (KeyValuePair<long, ChunkInfo> p in chunks)
        {
            int x, z;
            Decode(p.Key, out x, out z);
            Vector2Int c = new Vector2Int(x, z);
            DrawCell(c, size, TierColor(p.Value.MaxTier), true);
#if UNITY_EDITOR
            if (DrawLabels)
            {
                Handles.color = Color.white;
                Handles.Label(OUTL_ChunkProcessingDriver.ChunkCenter(c, size, YOffset + 0.25f), c.x + ":" + c.y + " count=" + p.Value.Count + " " + p.Value.MaxTier);
            }
#endif
        }
    }

    private void DrawEntityMarkers()
    {
        for (int i = 0; i < entities.Count; i++)
        {
            OUTL_EntityRuntime e = entities[i];
            if (e == null || e.Adapter == null) continue;
            Vector3 p = e.Adapter.transform.position + Vector3.up * 0.7f;
            Gizmos.color = TierColor(e.Tier);
            Gizmos.DrawSphere(p, 0.35f);
#if UNITY_EDITOR
            if (DrawLabels)
            {
                Handles.color = Color.white;
                Handles.Label(p + Vector3.up * 0.25f, e.Id + " " + e.ClassName + " " + e.Tier);
            }
#endif
        }
    }

    private void DrawCell(Vector2Int c, float size, Color color, bool filled)
    {
        Gizmos.color = color;
        Vector3 center = OUTL_ChunkProcessingDriver.ChunkCenter(c, size, YOffset);
        Vector3 s = new Vector3(size, 0.04f, size);
        if (filled) Gizmos.DrawCube(center, s);
        Gizmos.DrawWireCube(center, s);
    }

    private void DrawTierStats()
    {
        int full = 0, near = 0, mid = 0, far = 0, dormant = 0;
        for (int i = 0; i < entities.Count; i++)
        {
            switch (entities[i].Tier)
            {
                case OUTL_RuntimeTier.Full: full++; break;
                case OUTL_RuntimeTier.Near: near++; break;
                case OUTL_RuntimeTier.Mid: mid++; break;
                case OUTL_RuntimeTier.Far: far++; break;
                default: dormant++; break;
            }
        }
        GUILayout.Label("tiers: full=" + full + " near=" + near + " mid=" + mid + " far=" + far + " dormant=" + dormant);
    }

    private void ResolveRefs()
    {
        if (Driver == null) Driver = GetComponent<OUTL_ChunkProcessingDriver>();
        if (Focus == null && Driver != null && Driver.Focus != null) Focus = Driver.Focus;
        if (Focus != null || !UseRegistryFocusFallback) return;

        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;

        OUTL_EntityRuntime runtime = null;
        if (!string.IsNullOrEmpty(FocusTargetName))
            runtime = world.Registry.FindFirstByTargetName(FocusTargetName);
        if (runtime == null && !string.IsNullOrEmpty(FocusClassName))
            runtime = world.Registry.FindFirstByClassName(FocusClassName);
        if (runtime != null && runtime.Adapter != null)
        {
            Focus = runtime.Adapter.transform;
        }
    }

    private float GetChunkSize() { return Driver != null ? Mathf.Max(1f, Driver.ChunkSize) : Mathf.Max(1f, ChunkSize); }
    private Color TierColor(OUTL_RuntimeTier t) { switch (t) { case OUTL_RuntimeTier.Full: return FullColor; case OUTL_RuntimeTier.Near: return NearColor; case OUTL_RuntimeTier.Mid: return MidColor; case OUTL_RuntimeTier.Far: return FarColor; default: return DormantColor; } }
    private static long Key(int x, int z) { unchecked { return ((long)x << 32) ^ (uint)z; } }
    private static void Decode(long key, out int x, out int z) { x = (int)(key >> 32); z = (int)(key & 0xffffffff); }
}
