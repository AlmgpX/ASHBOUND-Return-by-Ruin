using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_PooledAutoRelease : MonoBehaviour
{
    public float Lifetime = 2f;
    public bool ReleaseOnDisable = false;

    private void OnEnable()
    {
        OUTL_PoolSystem.ReleaseShared(gameObject, Mathf.Max(0.01f, Lifetime));
    }

    private void OnDisable()
    {
        if (ReleaseOnDisable)
            OUTL_PoolSystem.ReleaseShared(gameObject);
    }
}
