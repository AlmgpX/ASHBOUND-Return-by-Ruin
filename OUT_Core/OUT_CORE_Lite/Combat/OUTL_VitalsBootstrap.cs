using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_VitalsBootstrap : MonoBehaviour
{
    public OUTL_EntityAdapter Entity;
    public bool AddVitalsIfMissing = false;
    public bool AddDeathHandlerIfMissing = false;
    public bool AddUIBinderIfPlayer = false;
    public string PlayerTag = "Player";
    public string PlayerClassName = "player";

    private bool missingVitalsWarned;
    private bool missingDeathWarned;
    private bool missingUIBinderWarned;

    private void Awake()
    {
        Ensure();
    }

    private void OnValidate()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    [ContextMenu("OUT Ensure Vitals Stack")]
    public void Ensure()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Entity == null) return;
        bool isPlayer = LooksLikePlayer(Entity, PlayerTag, PlayerClassName);

        OUTL_Vitals vitals = GetComponent<OUTL_Vitals>();
        if (vitals == null && AddVitalsIfMissing) WarnMissingVitals();
        if (vitals != null)
        {
            if (vitals.Entity == null) vitals.Entity = Entity;
            if (isPlayer)
            {
                vitals.DisableCharacterControllerWhenDead = false;
                vitals.BlockMovementWhenDead = true;
                vitals.BlockAttacksWhenDead = true;
            }
        }

        OUTL_DeathHandler death = GetComponent<OUTL_DeathHandler>();
        if (death == null && AddDeathHandlerIfMissing) WarnMissingDeathHandler();
        if (death != null)
        {
            if (death.Entity == null) death.Entity = Entity;
            if (isPlayer)
            {
                death.QueueDespawn = false;
                death.DisableRenderers = false;
                death.DisableColliders = false;
            }
        }

        if (AddUIBinderIfPlayer && isPlayer && GetComponentInChildren<OUTL_UIDataBinder>(true) == null)
            WarnMissingUIBinder();
    }

    private static bool LooksLikePlayer(OUTL_EntityAdapter entity, string playerTag, string playerClassName)
    {
        if (entity == null) return false;
        if (entity.Runtime != null && !string.IsNullOrEmpty(playerTag) && entity.Runtime.HasTag(playerTag)) return true;
        if (entity.Def != null && entity.Def.Tags != null)
            for (int i = 0; i < entity.Def.Tags.Length; i++)
                if (entity.Def.Tags[i] == playerTag) return true;
        string className = entity.Runtime != null ? entity.Runtime.ClassName : entity.ClassNameOverride;
        return !string.IsNullOrEmpty(playerClassName) && string.Equals(className, playerClassName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void WarnMissingVitals()
    {
        if (missingVitalsWarned) return;
        missingVitalsWarned = true;
        Debug.LogWarning("OUTL_VitalsBootstrap found no OUTL_Vitals. Add it in prefab/editor setup; runtime component construction is disabled by canon.", this);
    }

    private void WarnMissingDeathHandler()
    {
        if (missingDeathWarned) return;
        missingDeathWarned = true;
        Debug.LogWarning("OUTL_VitalsBootstrap found no OUTL_DeathHandler. Add it in prefab/editor setup; runtime component construction is disabled by canon.", this);
    }

    private void WarnMissingUIBinder()
    {
        if (missingUIBinderWarned) return;
        missingUIBinderWarned = true;
        Debug.LogWarning("OUTL_VitalsBootstrap found no OUTL_UIDataBinder for player UI. Add it in prefab/editor setup; runtime component construction is disabled by canon.", this);
    }
}
