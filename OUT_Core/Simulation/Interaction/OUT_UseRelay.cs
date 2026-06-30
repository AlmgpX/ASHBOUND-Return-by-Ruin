using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class OUT_UseRelay : MonoBehaviour, IOutUsable, IOutContinuousUsable
{
    [Header("Relay")]
    [SerializeField] private OUT_UseCapabilityFlags useCaps = OUT_UseCapabilityFlags.ImpulseUse;
    [SerializeField] private GameObject[] targetObjects;
    [SerializeField] private bool useSelfWhenNoTargets = false;
    [SerializeField] private bool stopOnFirstSuccess = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onUsePerformed;
    [SerializeField] private UnityEvent onUseFailed;

    private readonly List<MonoBehaviour> _activeContinuousTargets = new List<MonoBehaviour>(8);

    public OUT_UseCapabilityFlags UseCaps => useCaps;

    public bool CanUse(in OUT_UseRequest request)
    {
        OUT_UseRequest localRequest = request;
        return FindUsableTarget(localRequest, requireCanUse: true) != null;
    }

    public OUT_UseResult Use(in OUT_UseRequest request)
    {
        OUT_UseRequest localRequest = request;
        bool success = false;
        bool consumed = false;
        _activeContinuousTargets.Clear();

        bool hasExplicitTargets = targetObjects != null && targetObjects.Length > 0;
        if (hasExplicitTargets)
        {
            for (int i = 0; i < targetObjects.Length; i++)
            {
                GameObject targetObject = targetObjects[i];
                if (targetObject == null)
                    continue;

                if (ProcessUsablesOnObject(targetObject, localRequest, ref success, ref consumed) && stopOnFirstSuccess)
                    break;
            }
        }
        else if (useSelfWhenNoTargets)
        {
            ProcessUsablesOnObject(gameObject, localRequest, ref success, ref consumed);
        }

        if (success)
        {
            onUsePerformed?.Invoke();
            return OUT_UseResult.Performed(consumed, "Use relay performed");
        }

        onUseFailed?.Invoke();
        return OUT_UseResult.Failed("No target accepted use");
    }

    public bool CanContinueUse(in OUT_UseRequest request)
    {
        OUT_UseRequest localRequest = request;

        for (int i = _activeContinuousTargets.Count - 1; i >= 0; i--)
        {
            if (!(_activeContinuousTargets[i] is IOutContinuousUsable continuous) || _activeContinuousTargets[i] == null)
            {
                _activeContinuousTargets.RemoveAt(i);
                continue;
            }

            if (continuous.CanContinueUse(localRequest))
                return true;
        }

        return false;
    }

    public OUT_UseResult ContinueUse(in OUT_UseRequest request)
    {
        OUT_UseRequest localRequest = request;
        bool success = false;
        bool consumed = false;

        for (int i = _activeContinuousTargets.Count - 1; i >= 0; i--)
        {
            if (!(_activeContinuousTargets[i] is IOutContinuousUsable continuous) || _activeContinuousTargets[i] == null)
            {
                _activeContinuousTargets.RemoveAt(i);
                continue;
            }

            if (!continuous.CanContinueUse(localRequest))
                continue;

            OUT_UseResult result = continuous.ContinueUse(localRequest);
            if (!result.Success)
                continue;

            success = true;
            consumed |= result.Consumed;
        }

        if (success)
            return OUT_UseResult.Performed(consumed, "Use relay continued");

        return OUT_UseResult.Failed("No continuous target accepted use");
    }

    public void EndUse(in OUT_UseRequest request)
    {
        OUT_UseRequest localRequest = request;

        for (int i = _activeContinuousTargets.Count - 1; i >= 0; i--)
        {
            if (!(_activeContinuousTargets[i] is IOutContinuousUsable continuous) || _activeContinuousTargets[i] == null)
            {
                _activeContinuousTargets.RemoveAt(i);
                continue;
            }

            continuous.EndUse(localRequest);
        }

        _activeContinuousTargets.Clear();
    }

    private IOutUsable FindUsableTarget(OUT_UseRequest request, bool requireCanUse)
    {
        bool hasExplicitTargets = targetObjects != null && targetObjects.Length > 0;
        if (hasExplicitTargets)
        {
            for (int i = 0; i < targetObjects.Length; i++)
            {
                GameObject targetObject = targetObjects[i];
                if (targetObject == null)
                    continue;

                IOutUsable found = FindUsableOnObject(targetObject, request, requireCanUse);
                if (found != null)
                    return found;
            }

            return null;
        }

        if (useSelfWhenNoTargets)
            return FindUsableOnObject(gameObject, request, requireCanUse);

        return null;
    }

    private IOutUsable FindUsableOnObject(GameObject targetObject, OUT_UseRequest request, bool requireCanUse)
    {
        MonoBehaviour[] behaviours = targetObject.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this)
                continue;

            if (!(behaviour is IOutUsable usable))
                continue;

            if (requireCanUse && !usable.CanUse(request))
                continue;

            return usable;
        }

        return null;
    }

    private bool ProcessUsablesOnObject(GameObject targetObject, OUT_UseRequest request, ref bool success, ref bool consumed)
    {
        MonoBehaviour[] behaviours = targetObject.GetComponents<MonoBehaviour>();
        bool processed = false;

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this)
                continue;

            if (!(behaviour is IOutUsable usable))
                continue;

            if (!usable.CanUse(request))
                continue;

            OUT_UseResult result = usable.Use(request);
            if (!result.Success)
                continue;

            success = true;
            consumed |= result.Consumed;
            processed = true;

            if (usable is IOutContinuousUsable continuous &&
                (usable.UseCaps & OUT_UseCapabilityFlags.ContinuousUse) != 0 &&
                continuous is MonoBehaviour continuousBehaviour &&
                !_activeContinuousTargets.Contains(continuousBehaviour))
            {
                _activeContinuousTargets.Add(continuousBehaviour);
            }

            if (stopOnFirstSuccess)
                return true;
        }

        return processed;
    }
}
