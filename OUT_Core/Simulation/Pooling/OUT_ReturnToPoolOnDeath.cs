using System.Collections;
using OUTPool = OutCore.pool.OUT;
using UnityEngine;

[DisallowMultipleComponent]
public class OUT_ReturnToPoolOnDeath : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OUT_HealthSimple health;

    [Header("Timing")]
    [SerializeField][Min(0f)] private float returnDelay = 0.1f;

    private Coroutine _returnRoutine;

    private void Reset()
    {
        if (health == null)
            health = GetComponent<OUT_HealthSimple>();
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<OUT_HealthSimple>();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<OUT_HealthSimple>();

        if (health != null)
            health.Died += OnDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= OnDied;

        if (_returnRoutine != null)
        {
            StopCoroutine(_returnRoutine);
            _returnRoutine = null;
        }
    }

    private void OnDied(OUT_DamageContext context)
    {
        if (_returnRoutine != null)
            StopCoroutine(_returnRoutine);

        _returnRoutine = StartCoroutine(ReturnAfterDelay());
    }

    private IEnumerator ReturnAfterDelay()
    {
        if (returnDelay > 0f)
            yield return new WaitForSeconds(returnDelay);

        _returnRoutine = null;

        if (OUTPool.IsManaged(gameObject))
            OUTPool.Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
