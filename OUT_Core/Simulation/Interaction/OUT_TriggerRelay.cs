using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class OUT_TriggerRelay : MonoBehaviour, IOutTriggerReceiver
{
    [Header("Relay")]
    [SerializeField] private GameObject[] targetObjects;
    [SerializeField] private bool useSelfWhenNoTargets = false;
    [SerializeField] private bool stopOnFirstSuccess = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onTriggerPerformed;
    [SerializeField] private UnityEvent onTriggerFailed;

    public bool CanReceiveTrigger(in OUT_TriggerContext context)
    {
        OUT_TriggerContext localContext = context;
        return FindTriggerReceiver(localContext, requireCanReceive: true) != null;
    }

    public void ReceiveTrigger(in OUT_TriggerContext context)
    {
        OUT_TriggerContext localContext = context;
        bool success = false;

        bool hasExplicitTargets = targetObjects != null && targetObjects.Length > 0;
        if (hasExplicitTargets)
        {
            for (int i = 0; i < targetObjects.Length; i++)
            {
                GameObject targetObject = targetObjects[i];
                if (targetObject == null)
                    continue;

                if (ProcessTriggerReceiversOnObject(targetObject, localContext, ref success) && stopOnFirstSuccess)
                    break;
            }
        }
        else if (useSelfWhenNoTargets)
        {
            ProcessTriggerReceiversOnObject(gameObject, localContext, ref success);
        }

        if (success)
            onTriggerPerformed?.Invoke();
        else
            onTriggerFailed?.Invoke();
    }

    private IOutTriggerReceiver FindTriggerReceiver(OUT_TriggerContext context, bool requireCanReceive)
    {
        bool hasExplicitTargets = targetObjects != null && targetObjects.Length > 0;
        if (hasExplicitTargets)
        {
            for (int i = 0; i < targetObjects.Length; i++)
            {
                GameObject targetObject = targetObjects[i];
                if (targetObject == null)
                    continue;

                IOutTriggerReceiver found = FindTriggerReceiverOnObject(targetObject, context, requireCanReceive);
                if (found != null)
                    return found;
            }

            return null;
        }

        if (useSelfWhenNoTargets)
            return FindTriggerReceiverOnObject(gameObject, context, requireCanReceive);

        return null;
    }

    private IOutTriggerReceiver FindTriggerReceiverOnObject(GameObject targetObject, OUT_TriggerContext context, bool requireCanReceive)
    {
        MonoBehaviour[] behaviours = targetObject.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this)
                continue;

            if (!(behaviour is IOutTriggerReceiver receiver))
                continue;

            if (requireCanReceive && !receiver.CanReceiveTrigger(context))
                continue;

            return receiver;
        }

        return null;
    }

    private bool ProcessTriggerReceiversOnObject(GameObject targetObject, OUT_TriggerContext context, ref bool success)
    {
        MonoBehaviour[] behaviours = targetObject.GetComponents<MonoBehaviour>();
        bool processed = false;

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this)
                continue;

            if (!(behaviour is IOutTriggerReceiver receiver))
                continue;

            if (!receiver.CanReceiveTrigger(context))
                continue;

            receiver.ReceiveTrigger(context);
            success = true;
            processed = true;

            if (stopOnFirstSuccess)
                return true;
        }

        return processed;
    }
}
