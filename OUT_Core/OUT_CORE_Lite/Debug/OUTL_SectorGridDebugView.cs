using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable 0618

[ExecuteAlways]
[DisallowMultipleComponent]
public class OUTL_SectorGridDebugView : MonoBehaviour
{
    [Header("Source")]
    public OUTL_ChunkProcessingDriver ChunkDriver;
    [FormerlySerializedAs("ProcessingDriver")]
    public OUTL_ProcessingDistanceDriver LegacyProcessingDriver;
    public OUTL_ProcessingProfileAsset ProfileAsset;
    public Transform Focus;
    [Tooltip("Legacy compatibility field. Runtime scene camera lookup is intentionally not used by this debug view.")]
    public bool UseMainCameraAsFocus = false;
    [Tooltip("Legacy compatibility field. Runtime Unity tag lookup is intentionally not used by this debug view.")]
    public bool UsePlayerTagAsFallback = false;
    public bool UseRegistryFocusFallback = true;
    public string FocusTargetName = "player";
    public string FocusClassName = "player";

    [Header("Grid")]
    public bool DrawGrid = true;
    public bool DrawOnlyOccupiedCells = false;
    public bool DrawOccupiedCells = true;
    public bool DrawFocusCell = true;
    public int CellRadius = 8;
    public float FallbackCellSize = 32f;
    public float YOffset = 0.05f;
    public bool UseFocusY = false;

    [Header("Entities")]
    public bool DrawEntityMarkers = true;
    public bool DrawEntityLabels = false;
    public bool DrawCellLabels = false;
    public float EntityMarkerSize = 0.45f;

    [Header("Overlay")]
    public bool ShowOverlay = true;
    public KeyCode ToggleOverlayKey = KeyCode.F4;
    public Rect OverlayRect = new Rect(12, 382, 520, 180);

    [Header("Tier Rings")]
    public bool DrawTierRings = true;
    public int RingSegments = 96;
    public float RingYOffset = 0.12f;

    [Header("Colors")]
    public Color GridColor = new Color(0.2f, 0.55f, 1f, 0.16f);
    public Color OccupiedCellColor = new Color(0.2f, 1f, 0.45f, 0.32f);
    public Color FocusCellColor = new Color(1f, 0.9f, 0.25f, 0.5f);
    public Color DormantColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
    public Color FarColor = new Color(0.3f, 0.5f, 1f, 0.8f);
    public Color MidColor = new Color(0.35f, 1f, 0.65f, 0.8f);
    public Color NearColor = new Color(1f, 0.85f, 0.25f, 0.85f);
    public Color FullColor = new Color(1f, 0.25f, 0.18f, 0.9f);

    private readonly List<OUTL_EntityRuntime> entityBuffer = new List<OUTL_EntityRuntime>(1024);
    private readonly List<OUTL_SectorCellStats> sectorStats = new List<OUTL_SectorCellStats>(256);
    private readonly Dictionary<long, CellDebugInfo> occupied = new Dictionary<long, CellDebugInfo>(256);
    private bool overlayVisible = true;

    private struct CellDebugInfo
    {
        public int Count;
        public OUTL_RuntimeTier MaxTier;
    }

    private void Update()
    {
        if (Application.isPlaying && Input.GetKeyDown(ToggleOverlayKey)) overlayVisible = !overlayVisible;
    }

    private void OnGUI()
    {
        if (!ShowOverlay || !overlayVisible) return;
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;

        world.Registry.CopyAll(entityBuffer);
        world.Sectors.CopyCellStats(sectorStats);
        OUTL_SectorIntegrityStats integrity;
        world.Sectors.ValidateIntegrity(null, out integrity);

        int full = 0;
        int mid = 0;
        int far = 0;
        int dormant = 0;
        int near = 0;
        for (int i = 0; i < entityBuffer.Count; i++)
        {
            OUTL_EntityRuntime e = entityBuffer[i];
            if (e == null) continue;
            switch (e.Tier)
            {
                case OUTL_RuntimeTier.Full: full++; break;
                case OUTL_RuntimeTier.Near: near++; break;
                case OUTL_RuntimeTier.Mid: mid++; break;
                case OUTL_RuntimeTier.Far: far++; break;
                default: dormant++; break;
            }
        }

        float average = sectorStats.Count > 0 ? (float)integrity.SectorEntityCount / sectorStats.Count : 0f;
        GUILayout.BeginArea(OverlayRect, GUI.skin.box);
        GUILayout.Label("OUTL Sector Integrity");
        GUILayout.Label("cells=" + sectorStats.Count + " indexed=" + integrity.SectorEntityCount + " registry=" + integrity.RegistryEntityCount + " avg/cell=" + average.ToString("0.0") + " worst=" + integrity.WorstSectorEntityCount);
        GUILayout.Label("tiers: full=" + full + " near=" + near + " mid=" + mid + " far=" + far + " dormant=" + dormant);
        GUILayout.Label("integrity: missingSector=" + integrity.MissingFromSector + " missingRegistry=" + integrity.MissingFromRegistry + " dup=" + integrity.DuplicateSectorEntries + " stale=" + integrity.StaleSectorAddress);
        GUILayout.Label("F4 overlay");
        GUILayout.EndArea();
    }

    private void OnDrawGizmos()
    {
        DrawDebug(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawDebug(true);
    }

    [ContextMenu("Resolve Focus")]
    public void ResolveFocusNow()
    {
        ResolveFocus();
    }

    private void DrawDebug(bool selected)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;

        ResolveFocus();
        if (Focus == null) return;

        float cellSize = GetCellSize();
        if (cellSize <= 0.01f) cellSize = 32f;
        float y = UseFocusY ? Focus.position.y + YOffset : YOffset;
        Vector3 focusPos = Focus.position;
        Vector2Int focusCell = WorldToCell(focusPos, cellSize);

        BuildOccupiedMap(world, cellSize);

        if (DrawGrid)
            DrawGridCells(focusCell, cellSize, y);

        if (DrawOccupiedCells)
            DrawOccupied(cellSize, y);

        if (DrawFocusCell)
            DrawCell(focusCell, cellSize, y, FocusCellColor, true);

        if (DrawTierRings)
            DrawRings(focusPos, y + RingYOffset);

        if (DrawEntityMarkers)
            DrawEntities(y + 0.35f);
    }

    private void BuildOccupiedMap(OUTL_World world, float cellSize)
    {
        occupied.Clear();
        world.Registry.CopyAll(entityBuffer);

        for (int i = 0; i < entityBuffer.Count; i++)
        {
            OUTL_EntityRuntime e = entityBuffer[i];
            if (e == null || e.Adapter == null) continue;
            Vector2Int cell = WorldToCell(e.Adapter.transform.position, cellSize);
            long key = CellKey(cell.x, cell.y);

            CellDebugInfo info;
            if (!occupied.TryGetValue(key, out info))
            {
                info = new CellDebugInfo { Count = 0, MaxTier = OUTL_RuntimeTier.Dormant };
            }

            info.Count++;
            if ((int)e.Tier > (int)info.MaxTier) info.MaxTier = e.Tier;
            occupied[key] = info;
        }
    }

    private void DrawGridCells(Vector2Int focusCell, float cellSize, float y)
    {
        int radius = Mathf.Max(0, CellRadius);
        for (int z = -radius; z <= radius; z++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                Vector2Int cell = new Vector2Int(focusCell.x + x, focusCell.y + z);
                long key = CellKey(cell.x, cell.y);
                if (DrawOnlyOccupiedCells && !occupied.ContainsKey(key)) continue;
                DrawCell(cell, cellSize, y, GridColor, false);
            }
        }
    }

    private void DrawOccupied(float cellSize, float y)
    {
        foreach (KeyValuePair<long, CellDebugInfo> pair in occupied)
        {
            int x;
            int z;
            DecodeCellKey(pair.Key, out x, out z);
            Vector2Int cell = new Vector2Int(x, z);
            Color color = TierColor(pair.Value.MaxTier);
            color.a = Mathf.Max(color.a, OccupiedCellColor.a);
            DrawCell(cell, cellSize, y + 0.03f, color, true);

#if UNITY_EDITOR
            if (DrawCellLabels)
            {
                Vector3 center = CellCenter(cell, cellSize, y + 0.2f);
                Handles.color = Color.white;
                Handles.Label(center, cell.x + ":" + cell.y + " / " + pair.Value.Count + " / " + pair.Value.MaxTier);
            }
#endif
        }
    }

    private void DrawEntities(float y)
    {
        for (int i = 0; i < entityBuffer.Count; i++)
        {
            OUTL_EntityRuntime e = entityBuffer[i];
            if (e == null || e.Adapter == null) continue;
            Vector3 p = e.Adapter.transform.position;
            p.y += y;
            Gizmos.color = TierColor(e.Tier);
            Gizmos.DrawSphere(p, Mathf.Max(0.05f, EntityMarkerSize));

#if UNITY_EDITOR
            if (DrawEntityLabels)
            {
                Handles.color = Color.white;
                string def = e.Def != null ? e.Def.name : "null";
                Handles.Label(p + Vector3.up * 0.35f, e.Id + " / " + e.Tier + " / " + def);
            }
#endif
        }
    }

    private void DrawCell(Vector2Int cell, float cellSize, float y, Color color, bool filled)
    {
        Vector3 center = CellCenter(cell, cellSize, y);
        Vector3 size = new Vector3(cellSize, 0.04f, cellSize);
        Gizmos.color = color;
        if (filled) Gizmos.DrawCube(center, size);
        Gizmos.DrawWireCube(center, size);
    }

    private void DrawRings(Vector3 focusPos, float y)
    {
        OUTL_ProcessingProfile profile = GetProfile();
        if (profile == null) return;

        Vector3 center = focusPos;
        center.y = UseFocusY ? focusPos.y + y : y;
        DrawRing(center, profile.FullDistance, FullColor);
        DrawRing(center, profile.NearDistance, NearColor);
        DrawRing(center, profile.MidDistance, MidColor);
        DrawRing(center, profile.FarDistance, FarColor);
    }

    private void DrawRing(Vector3 center, float radius, Color color)
    {
        if (radius <= 0.01f) return;
        int segments = Mathf.Clamp(RingSegments, 12, 256);
        Gizmos.color = color;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = (Mathf.PI * 2f * i) / segments;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private OUTL_ProcessingProfile GetProfile()
    {
        if (ChunkDriver != null && ChunkDriver.Profile != null) return ChunkDriver.Profile;
        if (LegacyProcessingDriver != null && LegacyProcessingDriver.Profile != null) return LegacyProcessingDriver.Profile;
        if (ProfileAsset != null && ProfileAsset.Profile != null) return ProfileAsset.Profile;
        return null;
    }

    private float GetCellSize()
    {
        if (ChunkDriver != null) return Mathf.Max(1f, ChunkDriver.ChunkSize);
        OUTL_ProcessingProfile profile = GetProfile();
        if (profile != null) return Mathf.Max(1f, profile.SectorCellSize);
        return Mathf.Max(1f, FallbackCellSize);
    }

    private void ResolveFocus()
    {
        if (Focus != null) return;
        if (ChunkDriver == null) ChunkDriver = GetComponent<OUTL_ChunkProcessingDriver>();
        if (LegacyProcessingDriver == null) LegacyProcessingDriver = GetComponent<OUTL_ProcessingDistanceDriver>();

        if (ChunkDriver != null && ChunkDriver.Focus != null)
        {
            Focus = ChunkDriver.Focus;
            return;
        }

        if (LegacyProcessingDriver != null && LegacyProcessingDriver.Focus != null)
        {
            Focus = LegacyProcessingDriver.Focus;
            return;
        }

        if (!UseRegistryFocusFallback) return;
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

    private static Vector2Int WorldToCell(Vector3 position, float cellSize)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));
    }

    private static Vector3 CellCenter(Vector2Int cell, float cellSize, float y)
    {
        return new Vector3((cell.x + 0.5f) * cellSize, y, (cell.y + 0.5f) * cellSize);
    }

    private static long CellKey(int x, int z)
    {
        unchecked { return ((long)x << 32) ^ (uint)z; }
    }

    private static void DecodeCellKey(long key, out int x, out int z)
    {
        x = (int)(key >> 32);
        z = (int)(key & 0xffffffff);
    }

    private Color TierColor(OUTL_RuntimeTier tier)
    {
        switch (tier)
        {
            case OUTL_RuntimeTier.Full: return FullColor;
            case OUTL_RuntimeTier.Near: return NearColor;
            case OUTL_RuntimeTier.Mid: return MidColor;
            case OUTL_RuntimeTier.Far: return FarColor;
            default: return DormantColor;
        }
    }
}

#pragma warning restore 0618
