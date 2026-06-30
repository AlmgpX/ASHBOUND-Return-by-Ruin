using UnityEngine;

public class OUT_RuntimePoolMember : MonoBehaviour
{
    public GameObject SourcePrefab;

    private IOutPoolResettable[] _resettables;

    public IOutPoolResettable[] Resettables
    {
        get
        {
            if (_resettables == null)
                RebuildResettableCache();

            return _resettables;
        }
    }

    public void RebuildResettableCache()
    {
        _resettables = GetComponentsInChildren<IOutPoolResettable>(true);
    }
}
