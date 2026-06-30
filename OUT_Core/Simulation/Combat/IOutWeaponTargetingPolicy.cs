using UnityEngine;

public interface IOutWeaponTargetingPolicy
{
    bool TryBuildAimContext(GameObject instigator, UnityEngine.Transform fireOrigin, out OUT_WeaponAimContext context);
}