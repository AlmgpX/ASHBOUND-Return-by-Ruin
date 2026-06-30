using UnityEngine;

[DisallowMultipleComponent]
public class OUT_AIDistanceActivator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform distanceTarget;
    [SerializeField] private bool useMainCameraIfMissing = true;

    [Header("Distances")]
    [SerializeField][Min(1f)] private float activeDistance = 180f;
    [SerializeField][Min(1f)] private float sleepDistance = 260f;
    [SerializeField][Min(0.05f)] private float checkInterval = 0.5f;

    [Header("Behaviours")]
    [SerializeField] private Behaviour[] behavioursToToggle;
    [SerializeField] private OUT_WeaponController[] weaponsToStop;

    private bool _isSleeping;
    private float _nextCheckTime;

    private void Awake()
    {
        if (distanceTarget == null)
            ResolveTarget();
    }

    private void Update()
    {
        if (Time.time < _nextCheckTime)
            return;

        _nextCheckTime = Time.time + checkInterval + Random.Range(0f, 0.15f);

        if (distanceTarget == null)
            ResolveTarget();

        if (distanceTarget == null)
            return;

        float distance = Vector3.Distance(transform.position, distanceTarget.position);

        if (!_isSleeping && distance >= sleepDistance)
            SetSleeping(true);
        else if (_isSleeping && distance <= activeDistance)
            SetSleeping(false);
    }

    private void SetSleeping(bool sleeping)
    {
        _isSleeping = sleeping;

        if (behavioursToToggle != null)
        {
            for (int i = 0; i < behavioursToToggle.Length; i++)
            {
                Behaviour behaviour = behavioursToToggle[i];
                if (behaviour == null || behaviour == this)
                    continue;

                behaviour.enabled = !sleeping;
            }
        }

        if (sleeping && weaponsToStop != null)
        {
            for (int i = 0; i < weaponsToStop.Length; i++)
            {
                if (weaponsToStop[i] != null)
                    weaponsToStop[i].StopActiveBursts();
            }
        }
    }

    private void ResolveTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            distanceTarget = player.transform;
            return;
        }

        if (useMainCameraIfMissing && Camera.main != null)
            distanceTarget = Camera.main.transform;
    }
}
