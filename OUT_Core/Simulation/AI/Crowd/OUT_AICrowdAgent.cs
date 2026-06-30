using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AICrowdAgent : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int stableSeed = 0;
    [SerializeField] private bool autoSeedFromName = true;

    [Header("Goal Spread")]
    [SerializeField][Min(0f)] private float goalSpreadRadius = 1.5f;
    [SerializeField][Min(1)] private int spreadRingCapacity = 6;

    [Header("Local Separation")]
    [SerializeField][Min(0f)] private float separationRadius = 1.25f;
    [SerializeField][Range(0f, 4f)] private float separationStrength = 0.75f;

    public int StableSeed => stableSeed;
    public float GoalSpreadRadius => goalSpreadRadius;
    public int SpreadRingCapacity => Mathf.Max(1, spreadRingCapacity);
    public float SeparationRadius => separationRadius;
    public float SeparationStrength => separationStrength;

    private void Reset()
    {
        RebuildSeed();
    }

    private void Awake()
    {
        if (stableSeed == 0 && autoSeedFromName)
            RebuildSeed();
    }

    private void OnEnable()
    {
        TryRegister();
    }

    private void Start()
    {
        TryRegister();
    }

    private void OnDisable()
    {
        if (OUT_AICrowdService.Instance != null)
            OUT_AICrowdService.Instance.Unregister(this);
    }

    [ContextMenu("Rebuild Stable Seed")]
    public void RebuildSeed()
    {
        stableSeed = ComputeStableHash(BuildSeedSource());
        if (stableSeed == 0)
            stableSeed = 1;
    }

    private void TryRegister()
    {
        if (OUT_AICrowdService.Instance != null)
            OUT_AICrowdService.Instance.Register(this);
    }

    private string BuildSeedSource()
    {
        Vector3 position = transform.position;
        int px = Mathf.RoundToInt(position.x * 100f);
        int py = Mathf.RoundToInt(position.y * 100f);
        int pz = Mathf.RoundToInt(position.z * 100f);

        string parentName = transform.parent != null ? transform.parent.name : "<root>";
        return gameObject.name + "|" + parentName + "|" + transform.GetSiblingIndex() + "|" + px + "|" + py + "|" + pz;
    }

    private int ComputeStableHash(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 1;

        unchecked
        {
            int hash = 23;
            for (int i = 0; i < value.Length; i++)
                hash = hash * 31 + value[i];

            return hash;
        }
    }
}
