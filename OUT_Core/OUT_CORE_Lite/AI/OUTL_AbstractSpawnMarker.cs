using System;
using UnityEngine;

[DefaultExecutionOrder(-8900)]
[DisallowMultipleComponent]
[AddComponentMenu("OUT CORE Lite/World/Abstract Spawn Marker")]
public sealed class OUTL_AbstractSpawnMarker : MonoBehaviour
{
    public OUTL_EntityDef EntityDef;
    public string StableId;
    public string TargetName;
    public string OutpostId;
    public bool CountsForOutpostClearance = true;
    public bool RegisterOnStart = true;
    public OUTL_RuntimeTier InitialTier = OUTL_RuntimeTier.Dormant;
    public Color GizmoColor = new Color(0.85f, 0.15f, 0.08f, 0.85f);

    public bool Registered { get; private set; }

    private void Reset()
    {
        EnsureStableId();
    }

    private void OnValidate()
    {
        EnsureStableId();
    }

    private void Start()
    {
        if (RegisterOnStart) RegisterNow();
    }

    [ContextMenu("OUTL Register Abstract Spawn")]
    public bool RegisterNow()
    {
        EnsureStableId();
        OUTL_World world = OUTL_World.Instance;
        if (world == null || EntityDef == null || EntityDef.Prefab == null || string.IsNullOrEmpty(StableId))
            return false;

        if (world.Materialization.ContainsStableId(StableId) || world.Registry.FindByStableId(StableId) != null)
        {
            Registered = true;
            return true;
        }

        Registered = world.Materialization.RegisterAbstractSpawn(
            EntityDef,
            transform.position,
            transform.rotation,
            StableId,
            InitialTier,
            TargetName,
            OutpostId);
        return Registered;
    }

    [ContextMenu("OUTL Regenerate Stable Id")]
    public void RegenerateStableId()
    {
        StableId = BuildStableId();
    }

    private void EnsureStableId()
    {
        if (string.IsNullOrEmpty(StableId)) StableId = BuildStableId();
    }

    private string BuildStableId()
    {
        string prefix = EntityDef != null && !string.IsNullOrEmpty(EntityDef.ClassName)
            ? EntityDef.ClassName
            : "entity";
        return prefix + ".marker." + Guid.NewGuid().ToString("N");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = GizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.55f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.25f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
