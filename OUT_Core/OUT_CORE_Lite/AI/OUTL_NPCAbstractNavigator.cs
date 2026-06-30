using UnityEngine;

public sealed class OUTL_NPCAbstractNavigator
{
    public bool BeginOrContinue(OUTL_World world, OUTL_NPCBehaviorRuntime runtime, OUTL_NPCWorldRouteCache cache, OUTL_NPCNavigationProfile profile, Vector3 currentPosition, Vector3 targetPosition, string routeKey, float time)
    {
        if (runtime == null || cache == null) return false;
        if (profile != null && !profile.CanUseAbstractTravel) return false;
        if (runtime.Travel.RouteFailedUntil > time) return false;

        OUTL_NPCRouteRecord route = cache.GetOrCreateRoute(world, currentPosition, targetPosition, profile, routeKey, time);
        if (route == null || route.Distance <= 0.01f)
        {
            runtime.Travel.Mode = OUTL_NPCTravelMode.RouteFailed;
            runtime.Travel.RouteFailedUntil = time + (profile != null ? Mathf.Max(0.1f, profile.RepathCooldown) : 2f);
            return false;
        }

        if (runtime.Travel.RouteKey != route.Key)
        {
            runtime.Travel.Mode = OUTL_NPCTravelMode.Abstract;
            runtime.Travel.RouteKey = route.Key;
            runtime.Travel.StartSectorId = route.StartSector;
            runtime.Travel.TargetSectorId = route.EndSector;
            runtime.Travel.StartPosition = currentPosition;
            runtime.Travel.TargetPosition = targetPosition;
            runtime.Travel.AbstractPosition = currentPosition;
            runtime.Travel.RouteProgress = 0f;
            runtime.Travel.EstimatedArrivalTime = time + route.EstimatedTravelTime;
        }

        return true;
    }

    public bool Advance(OUTL_NPCBehaviorRuntime runtime, OUTL_NPCNavigationProfile profile, float deltaTime)
    {
        if (runtime == null || runtime.Travel.Mode != OUTL_NPCTravelMode.Abstract) return false;
        float speed = profile != null ? Mathf.Max(0.01f, profile.WalkSpeed * profile.AbstractTravelSpeedMultiplier) : 2.2f;
        float distance = Vector3.Distance(runtime.Travel.StartPosition, runtime.Travel.TargetPosition);
        if (distance <= 0.01f)
        {
            runtime.Travel.RouteProgress = 1f;
            runtime.Travel.AbstractPosition = runtime.Travel.TargetPosition;
            return true;
        }

        runtime.Travel.RouteProgress = Mathf.Clamp01(runtime.Travel.RouteProgress + Mathf.Max(0f, deltaTime) * speed / distance);
        runtime.RouteProgress = runtime.Travel.RouteProgress;
        runtime.AbstractPosition = Vector3.Lerp(runtime.Travel.StartPosition, runtime.Travel.TargetPosition, runtime.Travel.RouteProgress);
        runtime.Travel.AbstractPosition = runtime.AbstractPosition;
        return runtime.Travel.RouteProgress >= 1f;
    }

    public bool Materialize(Transform transform, OUTL_NPCBehaviorRuntime runtime)
    {
        if (transform == null || runtime == null) return false;
        if (runtime.Travel.Mode != OUTL_NPCTravelMode.Abstract)
        {
            runtime.LastExactPosition = transform.position;
            runtime.AbstractPosition = transform.position;
            runtime.Travel.AbstractPosition = transform.position;
            if (runtime.Travel.Mode == OUTL_NPCTravelMode.None) runtime.Travel.Mode = OUTL_NPCTravelMode.Exact;
            return false;
        }

        Vector3 p = runtime.AbstractPosition != Vector3.zero ? runtime.AbstractPosition : runtime.Travel.AbstractPosition;
        if (p != Vector3.zero) transform.position = p;
        runtime.LastExactPosition = transform.position;
        runtime.AbstractPosition = transform.position;
        runtime.Travel.AbstractPosition = transform.position;
        runtime.Travel.Mode = OUTL_NPCTravelMode.Exact;
        return true;
    }
}
