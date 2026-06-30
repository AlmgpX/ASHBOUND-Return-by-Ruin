using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_AIInterceptPlanner : MonoBehaviour
{
    public OUTL_AIActor Actor;
    public bool Enabled = true;
    [Range(0f, 1f)] public float InterceptChance = 0.35f;
    public float MinDistance = 3f;
    public float MaxDistance = 28f;
    public float PredictionTime = 0.65f;
    public float RandomSideOffset = 1.25f;
    public float ReplanInterval = 0.8f;
    public float HerdBreakRadius = 1.2f;
    public LayerMask BlockMask = ~0;
    public LayerMask HerdMask = ~0;

    private readonly Collider[] herdBuffer = new Collider[24];
    private OUTL_EntityId lastTarget;
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private Vector3 currentInterceptPoint;
    private float nextPlanTime;
    private bool hasPlan;

    private void Awake()
    {
        if (Actor == null) Actor = GetComponent<OUTL_AIActor>();
    }

    public Vector3 ResolveMoveTarget(OUTL_EntityRuntime target, Vector3 fallbackTarget, float time)
    {
        if (!Enabled || Actor == null || target == null || target.Adapter == null) return fallbackTarget;

        Vector3 targetPos = target.Adapter.transform.position;
        if (target.Id != lastTarget)
        {
            lastTarget = target.Id;
            lastTargetPosition = targetPos;
            targetVelocity = Vector3.zero;
            hasPlan = false;
            nextPlanTime = 0f;
        }
        else
        {
            float dt = Mathf.Max(0.02f, Time.deltaTime);
            Vector3 measured = (targetPos - lastTargetPosition) / dt;
            targetVelocity = Vector3.Lerp(targetVelocity, measured, 0.35f);
            lastTargetPosition = targetPos;
        }

        Vector3 selfPos = Actor.MoveRoot != null ? Actor.MoveRoot.position : transform.position;
        float distSqr = (selfPos - targetPos).sqrMagnitude;
        if (distSqr < MinDistance * MinDistance || distSqr > MaxDistance * MaxDistance) return fallbackTarget;

        if (time >= nextPlanTime || !hasPlan)
        {
            nextPlanTime = time + Mathf.Max(0.05f, ReplanInterval);
            int sourceId = Actor.Entity != null && Actor.Entity.Id.IsValid ? Actor.Entity.Id.Value : 0;
            int targetId = target.Id.Value;
            int planWindow = Mathf.FloorToInt(time / Mathf.Max(0.05f, ReplanInterval));
            hasPlan = OUTL_HumanRandom.Value01(0x1CE7CEu, sourceId, targetId + planWindow) <= InterceptChance;
            if (hasPlan) currentInterceptPoint = BuildInterceptPoint(selfPos, targetPos, targetVelocity, sourceId, targetId, planWindow);
        }

        if (!hasPlan) return fallbackTarget;
        return currentInterceptPoint;
    }

    private Vector3 BuildInterceptPoint(Vector3 selfPos, Vector3 targetPos, Vector3 velocity, int sourceId, int targetId, int planWindow)
    {
        Vector3 flatVel = velocity;
        flatVel.y = 0f;
        Vector3 predicted = targetPos + flatVel * PredictionTime;

        Vector3 toTarget = targetPos - selfPos;
        toTarget.y = 0f;
        Vector3 side = toTarget.sqrMagnitude > 0.001f ? Vector3.Cross(Vector3.up, toTarget.normalized) : transform.right;
        float sideRoll = OUTL_HumanRandom.Value01(0x51DEu, sourceId, targetId + planWindow);
        float sideSign = sideRoll < 0.5f ? -1f : 1f;
        float offset = OUTL_HumanRandom.Value01(0x0FF5E7u, targetId, sourceId + planWindow) * Mathf.Max(0f, RandomSideOffset);
        predicted += side * sideSign * offset;

        int localActors = CountNearbyActorsNonAlloc(selfPos);
        if (localActors > 1) predicted += side * sideSign * Mathf.Min(4f, localActors * 0.65f);

        Vector3 origin = selfPos + Vector3.up * 0.5f;
        Vector3 dir = predicted - selfPos;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist > 0.1f && Physics.Raycast(origin, dir / dist, dist, BlockMask, QueryTriggerInteraction.Ignore))
            return targetPos;

        return predicted;
    }

    private int CountNearbyActorsNonAlloc(Vector3 selfPos)
    {
        if (HerdBreakRadius <= 0f) return 0;
        int count = Physics.OverlapSphereNonAlloc(selfPos, HerdBreakRadius, herdBuffer, HerdMask, QueryTriggerInteraction.Ignore);
        int localActors = 0;
        for (int i = 0; i < count; i++)
        {
            Collider c = herdBuffer[i];
            herdBuffer[i] = null;
            OUTL_AIActor other = c != null ? c.GetComponentInParent<OUTL_AIActor>() : null;
            if (other != null && other != Actor) localActors++;
        }
        return localActors;
    }
}
