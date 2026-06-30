using System.Collections;
using OUTPool = OutCore.pool.OUT;
using UnityEngine;

public class OUT_RuntimePoolAutoReturn : MonoBehaviour, IOutPoolResettable, OUTL_IPoolReset
{
    [SerializeField] private float returnDelay = 2f;
    [SerializeField] private bool useParticleLifetime = true;

    private Coroutine _routine;

    private void OnEnable()
    {
        StartReturnCountdown();
    }

    public void OnTakenFromPool()
    {
        StartReturnCountdown();

        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;

            systems[i].Clear(true);
            systems[i].Play(true);
        }
    }

    public void OnReturnedToPool()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;

            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            systems[i].Clear(true);
        }
    }

    public void OUTL_OnPoolSpawn()
    {
        OnTakenFromPool();
    }

    public void OUTL_OnPoolRelease()
    {
        OnReturnedToPool();
    }

    private void StartReturnCountdown()
    {
        if (_routine != null)
            StopCoroutine(_routine);

        float delay = returnDelay;

        if (useParticleLifetime)
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null)
                    continue;

                var main = systems[i].main;
                float systemLifetime = main.duration + main.startLifetime.constantMax;
                delay = Mathf.Max(delay, systemLifetime);
            }
        }

        _routine = StartCoroutine(ReturnRoutine(delay));
    }

    private IEnumerator ReturnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (OUTPool.IsManaged(gameObject))
            OUTPool.Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
