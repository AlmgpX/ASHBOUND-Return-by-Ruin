using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_ProcessingTierController : MonoBehaviour, OUTL_IProcessingTierReceiver, OUTL_IPoolReset
{
    [Header("Minimum active tier")]
    public OUTL_RuntimeTier BehaviourMinimumTier = OUTL_RuntimeTier.Mid;
    public OUTL_RuntimeTier AnimatorMinimumTier = OUTL_RuntimeTier.Near;
    public OUTL_RuntimeTier AudioMinimumTier = OUTL_RuntimeTier.Near;
    public OUTL_RuntimeTier RendererMinimumTier = OUTL_RuntimeTier.Far;
    public OUTL_RuntimeTier GameObjectMinimumTier = OUTL_RuntimeTier.Near;

    [Header("Controlled targets")]
    [Tooltip("Legacy AI, sensors and other expensive scripts. Do not add OUTL_EntityAdapter or this controller.")]
    public Behaviour[] Behaviours;
    public Animator[] Animators;
    public AudioSource[] AudioSources;
    public Renderer[] Renderers;
    public GameObject[] GameObjects;

    private bool[] behaviourInitial;
    private bool[] animatorInitial;
    private bool[] audioInitial;
    private bool[] rendererInitial;
    private bool[] gameObjectInitial;
    private bool captured;
    private OUTL_RuntimeTier currentTier = OUTL_RuntimeTier.Full;

    private void Awake()
    {
        CaptureInitialState();
    }

    public void OUTL_OnProcessingTierChanged(OUTL_RuntimeTier oldTier, OUTL_RuntimeTier newTier, in OUTL_TierProcessingSettings settings)
    {
        if (!captured) CaptureInitialState();
        currentTier = newTier;
        ApplyTier(newTier);
    }

    public void OUTL_OnPoolSpawn()
    {
        if (!captured) CaptureInitialState();
        currentTier = OUTL_RuntimeTier.Full;
        ApplyTier(currentTier);
    }

    public void OUTL_OnPoolRelease()
    {
        if (!captured) CaptureInitialState();
        currentTier = OUTL_RuntimeTier.Full;
        ApplyTier(currentTier);
    }

    [ContextMenu("Capture Initial State")]
    public void CaptureInitialState()
    {
        behaviourInitial = CaptureEnabled(Behaviours);
        animatorInitial = CaptureEnabled(Animators);
        audioInitial = CaptureEnabled(AudioSources);
        rendererInitial = CaptureRendererEnabled(Renderers);
        gameObjectInitial = CaptureActive(GameObjects);
        captured = true;
    }

    [ContextMenu("Apply Current Tier")]
    public void ApplyCurrentTier()
    {
        ApplyTier(currentTier);
    }

    private void ApplyTier(OUTL_RuntimeTier tier)
    {
        SetEnabled(Behaviours, behaviourInitial, IsAtLeast(tier, BehaviourMinimumTier), this);
        SetEnabled(Animators, animatorInitial, IsAtLeast(tier, AnimatorMinimumTier), null);
        SetEnabled(AudioSources, audioInitial, IsAtLeast(tier, AudioMinimumTier), null);
        SetRendererEnabled(Renderers, rendererInitial, IsAtLeast(tier, RendererMinimumTier));
        SetActive(GameObjects, gameObjectInitial, IsAtLeast(tier, GameObjectMinimumTier), gameObject);
    }

    private static bool IsAtLeast(OUTL_RuntimeTier tier, OUTL_RuntimeTier minimum)
    {
        return (int)tier >= (int)minimum;
    }

    private static bool[] CaptureEnabled<T>(T[] targets) where T : Behaviour
    {
        if (targets == null) return null;
        bool[] result = new bool[targets.Length];
        for (int i = 0; i < targets.Length; i++)
            result[i] = targets[i] != null && targets[i].enabled;
        return result;
    }

    private static bool[] CaptureActive(GameObject[] targets)
    {
        if (targets == null) return null;
        bool[] result = new bool[targets.Length];
        for (int i = 0; i < targets.Length; i++)
            result[i] = targets[i] != null && targets[i].activeSelf;
        return result;
    }

    private static bool[] CaptureRendererEnabled(Renderer[] targets)
    {
        if (targets == null) return null;
        bool[] result = new bool[targets.Length];
        for (int i = 0; i < targets.Length; i++)
            result[i] = targets[i] != null && targets[i].enabled;
        return result;
    }

    private static void SetEnabled<T>(T[] targets, bool[] initial, bool tierActive, Behaviour excluded) where T : Behaviour
    {
        if (targets == null || initial == null) return;
        int count = Mathf.Min(targets.Length, initial.Length);
        for (int i = 0; i < count; i++)
        {
            T target = targets[i];
            if (target == null || ReferenceEquals(target, excluded)) continue;
            bool enabled = tierActive && initial[i];
            if (target.enabled != enabled) target.enabled = enabled;
        }
    }

    private static void SetActive(GameObject[] targets, bool[] initial, bool tierActive, GameObject excluded)
    {
        if (targets == null || initial == null) return;
        int count = Mathf.Min(targets.Length, initial.Length);
        for (int i = 0; i < count; i++)
        {
            GameObject target = targets[i];
            if (target == null || target == excluded) continue;
            bool active = tierActive && initial[i];
            if (target.activeSelf != active) target.SetActive(active);
        }
    }

    private static void SetRendererEnabled(Renderer[] targets, bool[] initial, bool tierActive)
    {
        if (targets == null || initial == null) return;
        int count = Mathf.Min(targets.Length, initial.Length);
        for (int i = 0; i < count; i++)
        {
            Renderer target = targets[i];
            if (target == null) continue;
            bool enabled = tierActive && initial[i];
            if (target.enabled != enabled) target.enabled = enabled;
        }
    }
}
