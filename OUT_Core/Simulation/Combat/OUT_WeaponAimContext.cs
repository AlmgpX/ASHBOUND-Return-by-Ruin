using UnityEngine;

[System.Serializable]
public struct OUT_WeaponAimContext
{
    public GameObject Instigator;
    public Transform FireOrigin;
    public Camera AimCamera;

    public GameObject Target;
    public Vector3 Origin;
    public Vector3 AimPoint;
    public Vector3 AimDirection;
    public Vector3 TargetVelocity;

    public float DistanceToAimPoint;
    public bool HasTarget;
    public bool HasDirectHit;
    public RaycastHit DirectHit;

    public bool IsValid
    {
        get
        {
            return FireOrigin != null && AimDirection.sqrMagnitude > 0.0001f;
        }
    }
}