using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_Interactable : MonoBehaviour, OUTL_IComponentSaveParticipant
{
    [Header("Localization Keys")]
    public string DisplayNameKey = "interact.object.name";
    public string DescriptionKey = "interact.object.description";
    public string VerbKey = "";

    [Header("Fallback Text")]
    public string DisplayName = "Use";
    [TextArea] public string DescriptionRu = "Использовать";
    [TextArea] public string DescriptionEn = "Use";
    public Sprite PromptIcon;

    [Header("OUTL Command")]
    public OUTL_EntityAdapter Entity;
    public OUTL_CommandType Command = OUTL_CommandType.Use;
    public bool SendToSelf = true;

    [Header("Canonical Outputs")]
    public OUTL_OutputLink[] Outputs;
    public string UsedEventName = "OnUsed";
    public string PickedUpEventName = "OnPickedUp";
    public string DroppedEventName = "OnDropped";

    [Header("Legacy Direct References")]
    [Tooltip("Migration data only. Runtime ignores direct Targets; use Outputs -> TargetName -> CommandSystem.")]
    public bool AllowLegacyDirectTargets;
    public OUTL_EntityAdapter[] Targets;
    [Tooltip("Migration data only. Runtime ignores UnityEvent callbacks; use Outputs instead.")]
    public bool InvokeLegacyUnityEvents;
    public UnityEngine.Events.UnityEvent OnUsed;

    [Header("Old OUT Interact Meaning")]
    public bool IsPhysicalProp;
    public bool OnlyPush;
    public float PushPower = 1f;
    public bool CanPickup = true;
    public bool UsePhysicalPropsLayerCheck = true;
    public string PhysicalPropsLayer = "PhysicalProps";
    public string HandledPropsLayer = "HandledProps";
    public UnityEngine.Events.UnityEvent OnPickedUp;
    public UnityEngine.Events.UnityEvent OnDropped;

    [Header("Audio")]
    public OUTL_AudioProfile UseAudio;
    public OUTL_AudioProfile PickupAudio;
    public OUTL_AudioProfile DropAudio;

    public string OUTL_SaveKey { get { return "OUTL_Interactable"; } }

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    public string GetDisplayName()
    {
        return OUTL_LanguageService.GetText(DisplayNameKey, DisplayName);
    }

    public string GetDescription()
    {
        string fallback = !string.IsNullOrEmpty(DescriptionRu) ? DescriptionRu : (!string.IsNullOrEmpty(DescriptionEn) ? DescriptionEn : DisplayName);
        return OUTL_LanguageService.GetText(DescriptionKey, fallback);
    }

    public string GetVerb()
    {
        string key = !string.IsNullOrEmpty(VerbKey) ? VerbKey : GetDefaultVerbKey();
        return OUTL_LanguageService.GetText(key, GetFallbackVerbRu());
    }

    public string GetDefaultVerbKey()
    {
        if (OnlyPush) return "interact.verb.push";
        if (IsPhysicalProp && CanPickup) return "interact.verb.pickup";
        switch (Command)
        {
            case OUTL_CommandType.Open: return "interact.verb.open";
            case OUTL_CommandType.Close: return "interact.verb.close";
            case OUTL_CommandType.Activate: return "interact.verb.activate";
            case OUTL_CommandType.Talk: return "interact.verb.talk";
            default: return "interact.verb.use";
        }
    }

    public string GetFallbackVerbRu()
    {
        if (OnlyPush) return "Толкнуть";
        if (IsPhysicalProp && CanPickup) return "Взять";
        switch (Command)
        {
            case OUTL_CommandType.Open: return "Открыть";
            case OUTL_CommandType.Close: return "Закрыть";
            case OUTL_CommandType.Activate: return "Активировать";
            case OUTL_CommandType.Talk: return "Говорить";
            default: return "Использовать";
        }
    }

    public bool IsPickupLayerAllowed(Rigidbody rb, Collider hitCollider)
    {
        if (!UsePhysicalPropsLayerCheck) return true;
        int physicalLayer = LayerMask.NameToLayer(PhysicalPropsLayer);
        if (physicalLayer < 0) return true;
        if (hitCollider != null && hitCollider.gameObject.layer == physicalLayer) return true;
        return rb != null && rb.gameObject.layer == physicalLayer;
    }

    public int GetHandledLayer()
    {
        return LayerMask.NameToLayer(HandledPropsLayer);
    }

    public bool CanUse(OUTL_EntityId source)
    {
        if (!isActiveAndEnabled) return false;

        bool hasOutputs = Outputs != null && Outputs.Length > 0;
        bool hasLegacyUnityEvent = InvokeLegacyUnityEvents && OnUsed != null && OnUsed.GetPersistentEventCount() > 0;
        if (!SendToSelf)
            return hasOutputs || hasLegacyUnityEvent || Entity == null;

        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Entity == null)
            return hasOutputs || hasLegacyUnityEvent;

        OUTL_Command command = new OUTL_Command(Command, source, Entity.Id) { Point = transform.position };
        OUTL_World world = OUTL_World.Instance;
        OUTL_ICommandReceiver[] receivers = Entity.CommandReceivers;
        if (receivers != null)
        {
            for (int i = 0; i < receivers.Length; i++)
                if (receivers[i] != null && receivers[i].OUTL_CanReceive(command, world))
                    return true;
        }

        return hasOutputs || hasLegacyUnityEvent;
    }

    public void Use(OUTL_EntityId source)
    {
        OUTL_World world = OUTL_World.Instance;
        if (world == null) return;
        if (!CanUse(source)) return;

        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        if (Entity != null && Entity.Runtime == null)
        {
            Entity.RebuildCommandReceiverCache();
            Entity.RebuildCommandGuardCache();
            Entity.RegisterNow(world);
        }

        if (SendToSelf && Entity != null)
            world.Commands.Send(new OUTL_Command(Command, source, Entity.Id) { Point = transform.position });

        OUTL_OutputDispatcher.Fire(world, source, this, transform.position, Outputs, UsedEventName, UsedEventName, (int)Command, true);

        if (UseAudio != null) UseAudio.Play(transform.position);
    }

    public void NotifyPickedUp()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        OUTL_World world = OUTL_World.Instance;
        OUTL_EntityId source = Entity != null ? Entity.Id : OUTL_EntityId.None;
        if (world != null) OUTL_OutputDispatcher.Fire(world, source, this, transform.position, Outputs, PickedUpEventName, PickedUpEventName, 0, true);
        if (PickupAudio != null) PickupAudio.Play(transform.position);
    }

    public void NotifyDropped()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
        OUTL_World world = OUTL_World.Instance;
        OUTL_EntityId source = Entity != null ? Entity.Id : OUTL_EntityId.None;
        if (world != null) OUTL_OutputDispatcher.Fire(world, source, this, transform.position, Outputs, DroppedEventName, DroppedEventName, 0, true);
        if (DropAudio != null) DropAudio.Play(transform.position);
    }

    public void OUTL_Capture(OUTL_ComponentSaveWriter writer)
    {
        OUTL_OutputSaveUtility.Capture(writer, "outputs", Outputs);
    }

    public void OUTL_Restore(OUTL_ComponentSaveReader reader)
    {
        OUTL_OutputSaveUtility.Restore(reader, "outputs", Outputs);
    }
}
