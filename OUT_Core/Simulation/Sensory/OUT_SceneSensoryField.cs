using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class OUT_SceneSensoryField : MonoBehaviour
{
    public enum OUT_SceneLightingMode
    {
        None = 0,
        LightProbes = 1,
        SkyVisibility = 2,
        Hybrid = 3
    }

    public static OUT_SceneSensoryField Instance { get; private set; }

    [Header("Field Bounds")]
    [SerializeField] private Collider boundsSource;
    [SerializeField] private Vector3 manualBoundsCenter = Vector3.zero;
    [SerializeField] private Vector3 manualBoundsSize = new Vector3(64f, 16f, 64f);
    [SerializeField][Min(0.25f)] private float cellSize = 1f;

    [Header("Ground Probes")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float groundProbeStartPadding = 4f;
    [SerializeField] private float groundProbeDistance = 32f;
    [SerializeField] private float evaluationHeight = 0.5f;
    [SerializeField][Min(0.1f)] private float maxSafeDropHeight = 1.2f;

    [Header("Lighting")]
    [SerializeField] private OUT_SceneLightingMode lightingMode = OUT_SceneLightingMode.Hybrid;
    [SerializeField][Range(0f, 4f)] private float probeLuminanceScale = 1f;
    [SerializeField][Range(0f, 1f)] private float hybridProbeWeight = 0.65f;
    [SerializeField][Range(0f, 1f)] private float hybridSkyWeight = 0.35f;
    [SerializeField][Min(0.25f)] private float skyProbeDistance = 6f;

    [Header("Spatial Heuristics")]
    [SerializeField][Min(0.25f)] private float occlusionProbeDistance = 2f;
    [SerializeField][Min(0.25f)] private float coverProbeDistance = 1.5f;

    [Header("Dynamic Stimuli")]
    [SerializeField] private bool includeDynamicStimuli = true;
    [SerializeField] private OUT_SceneStimulusService stimulusService;

    [Header("Build")]
    [SerializeField] private bool rebuildOnAwake = true;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private OUT_SensoryChannelFlags debugChannel = OUT_SensoryChannelFlags.Luminance;
    [SerializeField][Range(0.05f, 1f)] private float debugCubeScale = 0.25f;
    [SerializeField][Range(0.05f, 1f)] private float debugAlpha = 0.35f;

    [SerializeField, HideInInspector] private int width;
    [SerializeField, HideInInspector] private int depth;
    [SerializeField, HideInInspector] private Vector3 fieldOrigin;
    [SerializeField, HideInInspector] private OUT_SceneSensorySample[] samples;

    public bool HasBakedData => samples != null && samples.Length > 0 && width > 0 && depth > 0;

    private static readonly Vector3[] SkyDirections =
    {
        Vector3.up,
        new Vector3( 0.65f, 1f,  0.00f).normalized,
        new Vector3(-0.65f, 1f,  0.00f).normalized,
        new Vector3( 0.00f, 1f,  0.65f).normalized,
        new Vector3( 0.00f, 1f, -0.65f).normalized,
        new Vector3( 0.45f, 1f,  0.45f).normalized,
        new Vector3(-0.45f, 1f,  0.45f).normalized,
        new Vector3( 0.45f, 1f, -0.45f).normalized,
        new Vector3(-0.45f, 1f, -0.45f).normalized
    };

    private static readonly Vector3[] HorizontalDirections =
    {
        Vector3.forward,
        (Vector3.forward + Vector3.right).normalized,
        Vector3.right,
        (Vector3.back + Vector3.right).normalized,
        Vector3.back,
        (Vector3.back + Vector3.left).normalized,
        Vector3.left,
        (Vector3.forward + Vector3.left).normalized
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple OUT_SceneSensoryField instances found. Keeping the first one.");
            return;
        }

        Instance = this;

        if (includeDynamicStimuli && stimulusService == null)
            stimulusService = FindObjectOfType<OUT_SceneStimulusService>();

        if (rebuildOnAwake)
            RebuildField();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    [ContextMenu("Rebuild Sensory Field")]
    public void RebuildField()
    {
        Bounds bounds = GetFieldBounds();

        width = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / cellSize));
        depth = Mathf.Max(1, Mathf.CeilToInt(bounds.size.z / cellSize));
        fieldOrigin = new Vector3(bounds.min.x, 0f, bounds.min.z);

        samples = new OUT_SceneSensorySample[width * depth];

        float topY = bounds.max.y + groundProbeStartPadding;
        float rayDistance = bounds.size.y + groundProbeStartPadding + groundProbeDistance;

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = ToIndex(x, z);
                OUT_SceneSensorySample sample = default;
                sample.Clear();

                Vector3 xz = GetCellWorldXZ(bounds, x, z);

                if (Physics.Raycast(
                    new Vector3(xz.x, topY, xz.z),
                    Vector3.down,
                    out RaycastHit groundHit,
                    rayDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
                {
                    Vector3 evalPoint = groundHit.point + Vector3.up * evaluationHeight;

                    sample.HasGround = true;
                    sample.GroundHeight = groundHit.point.y;
                    sample.Luminance = SampleLuminance(evalPoint);
                    sample.Occlusion = SampleOcclusion(evalPoint);
                    sample.Cover = SampleCover(evalPoint);
                    sample.GroundSafety = 1f;
                    sample.AreaCost = 0f;
                }

                sample.Clamp01();
                samples[index] = sample;
            }
        }

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = ToIndex(x, z);
                OUT_SceneSensorySample sample = samples[index];

                if (!sample.HasGround)
                {
                    samples[index] = sample;
                    continue;
                }

                float worstDrop = 0f;
                bool hadNeighbour = false;

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0)
                            continue;

                        int nx = x + dx;
                        int nz = z + dz;

                        if (!IsInside(nx, nz))
                        {
                            worstDrop = Mathf.Max(worstDrop, maxSafeDropHeight);
                            continue;
                        }

                        OUT_SceneSensorySample neighbour = samples[ToIndex(nx, nz)];
                        if (!neighbour.HasGround)
                        {
                            worstDrop = Mathf.Max(worstDrop, maxSafeDropHeight);
                            continue;
                        }

                        hadNeighbour = true;

                        float downwardDrop = sample.GroundHeight - neighbour.GroundHeight;
                        if (downwardDrop > 0f)
                            worstDrop = Mathf.Max(worstDrop, downwardDrop);
                    }
                }

                if (!hadNeighbour)
                {
                    sample.GroundSafety = 0f;
                    sample.AreaCost = 1f;
                }
                else
                {
                    sample.GroundSafety = 1f - Mathf.Clamp01(worstDrop / Mathf.Max(0.01f, maxSafeDropHeight));
                    sample.AreaCost = Mathf.Clamp01(1f - sample.GroundSafety);
                }

                sample.Clamp01();
                samples[index] = sample;
            }
        }
    }

    public bool TrySample(
        Vector3 worldPosition,
        out OUT_SceneSensorySample sample,
        OUT_SensoryChannelFlags channels = OUT_SensoryChannelFlags.All)
    {
        sample = default;
        sample.Clear();

        bool hasStatic = TrySampleStatic(worldPosition, out OUT_SceneSensorySample staticSample);
        if (hasStatic)
            sample = staticSample;

        if (includeDynamicStimuli && stimulusService != null)
        {
            OUT_SensoryChannelFlags dynamicChannels = channels & OUT_SensoryChannelFlags.AllDynamic;
            if (dynamicChannels != OUT_SensoryChannelFlags.None)
            {
                OUT_SceneSensorySample dynamicSample = stimulusService.Sample(worldPosition, dynamicChannels);
                sample.MaxFrom(dynamicSample, dynamicChannels);
            }
        }

        if (!hasStatic)
            return sample.HasAnyDynamicSignal();

        return true;
    }

    public bool TrySampleStatic(Vector3 worldPosition, out OUT_SceneSensorySample sample)
    {
        sample = default;
        sample.Clear();

        if (!HasBakedData)
            return false;

        if (!TryGetCell(worldPosition, out int x, out int z))
            return false;

        sample = samples[ToIndex(x, z)];
        return sample.HasGround;
    }

    public bool TryGetNearestGroundPoint(Vector3 worldPosition, out Vector3 groundPoint)
    {
        groundPoint = worldPosition;

        if (!TrySampleStatic(worldPosition, out OUT_SceneSensorySample sample) || !sample.HasGround)
            return false;

        groundPoint.y = sample.GroundHeight;
        return true;
    }

    public Bounds GetFieldBounds()
    {
        if (boundsSource != null)
            return boundsSource.bounds;

        Vector3 center = transform.TransformPoint(manualBoundsCenter);
        return new Bounds(center, manualBoundsSize);
    }

    private Vector3 GetCellWorldXZ(Bounds bounds, int x, int z)
    {
        float wx = bounds.min.x + (x + 0.5f) * cellSize;
        float wz = bounds.min.z + (z + 0.5f) * cellSize;
        return new Vector3(wx, 0f, wz);
    }

    private bool TryGetCell(Vector3 worldPosition, out int x, out int z)
    {
        float localX = worldPosition.x - fieldOrigin.x;
        float localZ = worldPosition.z - fieldOrigin.z;

        x = Mathf.FloorToInt(localX / cellSize);
        z = Mathf.FloorToInt(localZ / cellSize);

        return IsInside(x, z);
    }

    private bool IsInside(int x, int z)
    {
        return x >= 0 && x < width && z >= 0 && z < depth;
    }

    private int ToIndex(int x, int z)
    {
        return z * width + x;
    }

    private float SampleLuminance(Vector3 worldPosition)
    {
        switch (lightingMode)
        {
            case OUT_SceneLightingMode.None:
                return 0f;

            case OUT_SceneLightingMode.LightProbes:
                return SampleProbeLuminance(worldPosition);

            case OUT_SceneLightingMode.SkyVisibility:
                return SampleSkyVisibility(worldPosition);

            case OUT_SceneLightingMode.Hybrid:
            {
                float probe = SampleProbeLuminance(worldPosition);
                float sky = SampleSkyVisibility(worldPosition);

                float weightSum = Mathf.Max(0.0001f, hybridProbeWeight + hybridSkyWeight);
                float mixed = (probe * hybridProbeWeight + sky * hybridSkyWeight) / weightSum;
                return Mathf.Clamp01(mixed);
            }

            default:
                return 0f;
        }
    }

    private float SampleProbeLuminance(Vector3 worldPosition)
    {
        if (LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count == 0)
            return 0f;

        LightProbes.GetInterpolatedProbe(worldPosition, null, out SphericalHarmonicsL2 sh);

        float r = Mathf.Max(0f, sh[0, 0]);
        float g = Mathf.Max(0f, sh[1, 0]);
        float b = Mathf.Max(0f, sh[2, 0]);

        float luminance = (0.2126f * r + 0.7152f * g + 0.0722f * b) * probeLuminanceScale;
        return Mathf.Clamp01(luminance);
    }

    private float SampleSkyVisibility(Vector3 worldPosition)
    {
        int visible = 0;

        for (int i = 0; i < SkyDirections.Length; i++)
        {
            Vector3 direction = SkyDirections[i];
            if (!Physics.Raycast(
                worldPosition,
                direction,
                skyProbeDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                visible++;
            }
        }

        return visible / (float)SkyDirections.Length;
    }

    private float SampleOcclusion(Vector3 worldPosition)
    {
        int blocked = 0;

        for (int i = 0; i < HorizontalDirections.Length; i++)
        {
            if (Physics.Raycast(
                worldPosition,
                HorizontalDirections[i],
                occlusionProbeDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                blocked++;
            }
        }

        return blocked / (float)HorizontalDirections.Length;
    }

    private float SampleCover(Vector3 worldPosition)
    {
        int blocked = 0;
        Vector3 lowPoint = worldPosition + Vector3.down * (evaluationHeight * 0.35f);

        for (int i = 0; i < HorizontalDirections.Length; i++)
        {
            if (Physics.Raycast(
                lowPoint,
                HorizontalDirections[i],
                coverProbeDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                blocked++;
            }
        }

        return blocked / (float)HorizontalDirections.Length;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !HasBakedData)
            return;

        Bounds bounds = GetFieldBounds();
        float cubeSize = Mathf.Max(0.05f, cellSize * debugCubeScale);

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                OUT_SceneSensorySample sample = samples[ToIndex(x, z)];
                if (!sample.HasGround)
                    continue;

                Vector3 xz = GetCellWorldXZ(bounds, x, z);
                Vector3 pos = new Vector3(xz.x, sample.GroundHeight + 0.05f, xz.z);

                float value = sample.GetChannelValue(GetSingleDebugChannel(debugChannel));
                Color color = GetDebugColor(value);

                Gizmos.color = color;
                Gizmos.DrawCube(pos, new Vector3(cubeSize, 0.05f, cubeSize));
            }
        }
    }

    private OUT_SensoryChannelFlags GetSingleDebugChannel(OUT_SensoryChannelFlags flags)
    {
        if ((flags & OUT_SensoryChannelFlags.Luminance) != 0) return OUT_SensoryChannelFlags.Luminance;
        if ((flags & OUT_SensoryChannelFlags.Occlusion) != 0) return OUT_SensoryChannelFlags.Occlusion;
        if ((flags & OUT_SensoryChannelFlags.Cover) != 0) return OUT_SensoryChannelFlags.Cover;
        if ((flags & OUT_SensoryChannelFlags.GroundSafety) != 0) return OUT_SensoryChannelFlags.GroundSafety;
        if ((flags & OUT_SensoryChannelFlags.AreaCost) != 0) return OUT_SensoryChannelFlags.AreaCost;
        if ((flags & OUT_SensoryChannelFlags.Noise) != 0) return OUT_SensoryChannelFlags.Noise;
        if ((flags & OUT_SensoryChannelFlags.Danger) != 0) return OUT_SensoryChannelFlags.Danger;
        if ((flags & OUT_SensoryChannelFlags.Food) != 0) return OUT_SensoryChannelFlags.Food;
        if ((flags & OUT_SensoryChannelFlags.Fire) != 0) return OUT_SensoryChannelFlags.Fire;

        return OUT_SensoryChannelFlags.Luminance;
    }

    private Color GetDebugColor(float value)
    {
        value = Mathf.Clamp01(value);

        Color a = new Color(0f, 0f, 0f, debugAlpha);
        Color b = new Color(1f, 1f, 1f, debugAlpha);
        return Color.Lerp(a, b, value);
    }
}
