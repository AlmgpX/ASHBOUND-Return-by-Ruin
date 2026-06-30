using UnityEngine;

public interface OUTL_ILaunchableProjectile
{
    void OUTL_Launch(OUTL_EntityId source, OUTL_AttackProfile profile, Vector3 direction);
}
