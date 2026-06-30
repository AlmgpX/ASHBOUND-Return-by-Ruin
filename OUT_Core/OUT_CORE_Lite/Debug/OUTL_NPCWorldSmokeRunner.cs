using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_NPCWorldSmokeRunner : MonoBehaviour
{
    public bool RunOnStart;
    public bool LogSuccess = true;
    public bool CleanupAfterRun = true;

    private int failures;
    private readonly List<GameObject> createdObjects = new List<GameObject>(32);

    private void Start()
    {
        if (RunOnStart) RunAll();
    }

    [ContextMenu("OUT Run NPC World Smoke")]
    public void RunAll()
    {
        failures = 0;
        ClearSmokeObjects();
        OUTL_World world = EnsureWorld();
        OUTL_StimulusBus.Clear();

        SmokeDeathOnce(world);
        SmokeDebugHealthRows(world);
        SmokeClientReplicaAuthority(world);
        SmokeScheduleAndAbstractTravel(world);
        SmokeExactTravelDoesNotSnapBack(world);
        SmokeInputDrivenNpcMaterializesAbstractTravel(world);
        SmokeNavInputDoesNotStealNpcDestination(world);
        SmokeActorInputBridgeClearsOwnedNavOnNoFrame(world);
        SmokeInputDrivenNpcScheduleUsesActorInput(world);
        SmokeStimulusInterrupt(world);
        SmokeLootDropsOnce(world);
        SmokePickupAddsInventory(world);
        SmokeContainerOpensLootOnceAndPickupAddsInventory(world);
        SmokeDoorButtonLogicChain(world);
        SmokeAccessKeyDoor(world);
        SmokeLogicSaveRoundtrip(world);
        SmokeDeadNpcStopsSystems(world);
        SmokeSaveLoadRestoresNpcRuntime(world);
        SmokeSaveLoadRestoresDeathAndInventory(world);

        if (failures == 0 && LogSuccess) Debug.Log("[OUTL NPC World Smoke] OK", this);
        else if (failures > 0) Debug.LogError("[OUTL NPC World Smoke] failed=" + failures, this);
        if (CleanupAfterRun) ClearSmokeObjects();
    }

    [ContextMenu("OUT Clear Smoke Objects")]
    public void ClearSmokeObjects()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
            DestroySmokeObject(createdObjects[i]);
        createdObjects.Clear();

        GameObject[] all = GameObject.FindObjectsOfType<GameObject>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name.StartsWith("OUTL_Smoke_"))
                DestroySmokeObject(all[i]);
    }

    private void SmokeDeathOnce(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_Death_NPC", world, new Vector3(0f, 0f, 0f));
        OUTL_Combat.ApplyDamage(OUTL_EntityId.None, npc.Id, 150f, npc.transform.position, "smoke");
        if (!npc.Runtime.Dead || npc.Runtime.LifeState != OUTL_LifeState.Dead) Fail("death contract did not mark runtime dead");
        bool second = OUTL_DeathRuntime.TryKill(npc.Runtime, OUTL_EntityId.None, "again", npc.transform.position, world);
        if (second) Fail("death contract allowed second kill");
    }

    private void SmokeDebugHealthRows(OUTL_World world)
    {
        OUTL_EntityAdapter actor = CreateActor("Smoke_DebugHealth_Actor", world, new Vector3(0f, 0f, 2f));
        actor.StableId = "smoke.debug.health";
        actor.TargetName = "smoke.debug.health";
        actor.Runtime.ClassName = "actor_debug_health";
        actor.Runtime.Tier = OUTL_RuntimeTier.Near;

        OUTL_DebugHealthRow row;
        if (!OUTL_DebugHealthOverlay.TryBuildRow(actor.Runtime, out row)) { Fail("debug health row missing for actor with vitals"); return; }
        if (Mathf.Abs(row.Health - 100f) > 0.01f || Mathf.Abs(row.MaxHealth - 100f) > 0.01f) Fail("debug health row did not read hp/maxhp");
        if (row.Dead || row.LifeState == "DEAD") Fail("debug health row marked healthy actor dead");
        if (row.Tier != OUTL_RuntimeTier.Near.ToString()) Fail("debug health row did not expose runtime tier");

        OUTL_Combat.ApplyDamage(OUTL_EntityId.None, actor.Id, 35f, actor.transform.position, "smoke_debug_health");
        if (!OUTL_DebugHealthOverlay.TryBuildRow(actor.Runtime, out row)) { Fail("debug health row missing after damage"); return; }
        if (Mathf.Abs(row.Health - 65f) > 0.01f) Fail("debug health row did not update immediately after damage");

        OUTL_Combat.ApplyDamage(OUTL_EntityId.None, actor.Id, 200f, actor.transform.position, "smoke_debug_health_dead");
        if (!OUTL_DebugHealthOverlay.TryBuildRow(actor.Runtime, out row)) { Fail("debug health row missing after death"); return; }
        if (!row.Dead || row.LifeState != "DEAD") Fail("debug health row did not mark dead actor as DEAD");
    }

    private void SmokeClientReplicaAuthority(OUTL_World world)
    {
        OUTL_NetworkSession session = world.GetComponent<OUTL_NetworkSession>();
        if (session == null) session = world.gameObject.AddComponent<OUTL_NetworkSession>();
        session.Mode = OUTL_NetworkMode.Client;
        session.WorldIsClientReplica = true;
        session.WorldIsServerAuthority = false;

        OUTL_EntityAdapter npc = CreateActor("Smoke_ClientReplica_NPC", world, new Vector3(2f, 0f, 0f));
        npc.gameObject.AddComponent<OUTL_NetworkIdentityLite>().ServerOwned = true;
        bool applied = OUTL_Combat.ApplyDamage(OUTL_EntityId.None, npc.Id, 150f, npc.transform.position, "client");
        if (applied || npc.Runtime.Dead) Fail("client replica applied damage/death locally");
        session.StartOfflineWorld();
    }

    private void SmokeScheduleAndAbstractTravel(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_Schedule_NPC", world, new Vector3(0f, 0f, 8f));
        OUTL_NPCBehaviorController controller = npc.gameObject.AddComponent<OUTL_NPCBehaviorController>();
        controller.Entity = npc;
        controller.Model = ScriptableObject.CreateInstance<OUTL_NPCBehaviorModel>();
        controller.Model.Schedule = ScriptableObject.CreateInstance<OUTL_NPCScheduleDef>();
        controller.Model.NavigationProfile = ScriptableObject.CreateInstance<OUTL_NPCNavigationProfile>();
        controller.Model.Schedule.Entries = new[] { new OUTL_NPCScheduleEntry { EntryId = "travel", Action = OUTL_NPCScheduleActionType.TravelTo, TargetMode = OUTL_NPCScheduleTargetMode.FixedWorldPosition, TargetPosition = new Vector3(12f, 0f, 8f), StartTimeNormalized = 0f, EndTimeNormalized = 1f } };
        npc.Runtime.Tier = OUTL_RuntimeTier.Far;
        controller.OUTL_Tick(world, world.WorldTime, 5f);
        if (controller.Runtime.CurrentEntryId != "travel") Fail("schedule did not select travel entry");
        if (controller.Runtime.RouteProgress <= 0f) Fail("far NPC did not advance abstract route progress");
    }

    private void SmokeExactTravelDoesNotSnapBack(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_ExactNoSnap_NPC", world, new Vector3(6f, 0f, 12f));
        OUTL_NPCBehaviorController controller = npc.gameObject.AddComponent<OUTL_NPCBehaviorController>();
        controller.Entity = npc;
        controller.Model = ScriptableObject.CreateInstance<OUTL_NPCBehaviorModel>();
        controller.Model.Schedule = ScriptableObject.CreateInstance<OUTL_NPCScheduleDef>();
        controller.Model.NavigationProfile = ScriptableObject.CreateInstance<OUTL_NPCNavigationProfile>();
        controller.Model.Schedule.Entries = new[] { new OUTL_NPCScheduleEntry { EntryId = "exact_travel", Action = OUTL_NPCScheduleActionType.TravelTo, TargetMode = OUTL_NPCScheduleTargetMode.FixedWorldPosition, TargetPosition = new Vector3(12f, 0f, 12f), StartTimeNormalized = 0f, EndTimeNormalized = 1f } };
        npc.Runtime.Tier = OUTL_RuntimeTier.Full;
        controller.Runtime.CurrentTier = OUTL_RuntimeTier.Full;
        controller.Runtime.CurrentAction = OUTL_NPCScheduleActionType.TravelTo;
        controller.Runtime.CurrentTargetPosition = new Vector3(12f, 0f, 12f);
        controller.Runtime.AbstractPosition = new Vector3(1f, 0f, 12f);
        controller.Runtime.Travel.Mode = OUTL_NPCTravelMode.Exact;
        controller.Runtime.Travel.AbstractPosition = controller.Runtime.AbstractPosition;

        controller.OUTL_Tick(world, world.WorldTime + 0.5f, 0.1f);
        if (npc.transform.position.x < 5.9f) Fail("exact NPC snapped back to stale abstract position");
    }

    private void SmokeNavInputDoesNotStealNpcDestination(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_NavInputAuthority_NPC", world, new Vector3(0f, 0f, 14f));
        OUTL_NavMeshMover nav = npc.gameObject.AddComponent<OUTL_NavMeshMover>();
        nav.UseOUTLTick = false;
        OUTL_NavMoverInputSink sink = npc.gameObject.AddComponent<OUTL_NavMoverInputSink>();
        sink.Entity = npc;
        sink.NavMover = nav;
        sink.ZeroInputStopDelay = 0f;

        nav.SetDestination(npc.transform.position + Vector3.forward * 8f, "npc_behavior");
        OUTL_ActorInputFrame aimOnly = OUTL_ActorInputFrame.Empty(world.WorldTime);
        aimOnly.HasAimWorldPoint = true;
        aimOnly.AimWorldPoint = npc.transform.position + Vector3.forward * 3f;
        sink.OUTL_ApplyInput(aimOnly, world);
        if (!nav.HasDestination || nav.CurrentMovementAuthority != "npc_behavior") Fail("NavMoverInputSink stole/stopped npc_behavior destination on aim-only input");

        OUTL_ActorInputFrame move = OUTL_ActorInputFrame.Empty(world.WorldTime);
        move.Move = Vector2.right;
        sink.OUTL_ApplyInput(move, world);
        if (!nav.HasDestination || nav.CurrentMovementAuthority != "actor_input") Fail("NavMoverInputSink did not claim movement authority for move input");

        OUTL_ActorInputFrame idle = OUTL_ActorInputFrame.Empty(world.WorldTime);
        sink.OUTL_ApplyInput(idle, world);
        if (nav.HasDestination) Fail("NavMoverInputSink did not stop its own destination on zero input");
    }

    private void SmokeActorInputBridgeClearsOwnedNavOnNoFrame(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_BridgeClearsInput_NPC", world, new Vector3(0f, 0f, 14.75f));
        OUTL_NavMeshMover nav = npc.gameObject.AddComponent<OUTL_NavMeshMover>();
        nav.UseOUTLTick = false;
        OUTL_NavMoverInputSink sink = npc.gameObject.AddComponent<OUTL_NavMoverInputSink>();
        sink.Entity = npc;
        sink.NavMover = nav;
        sink.ZeroInputStopDelay = 0f;
        OUTL_BotInputDriver bot = npc.gameObject.AddComponent<OUTL_BotInputDriver>();
        bot.Entity = npc;
        OUTL_ActorControlBridge bridge = npc.gameObject.AddComponent<OUTL_ActorControlBridge>();
        bridge.Entity = npc;
        bridge.InputSourceBehaviour = bot;
        bridge.InputSinkBehaviours = new Behaviour[] { sink };
        bridge.UseUnityUpdateForLocalInput = false;

        OUTL_ActorInputFrame move = OUTL_ActorInputFrame.Empty(world.WorldTime);
        move.Move = Vector2.up;
        sink.OUTL_ApplyInput(move, world);
        if (!nav.HasDestination || nav.CurrentMovementAuthority != "actor_input") Fail("smoke setup failed: actor_input destination was not claimed");

        bridge.OUTL_Tick(world, world.WorldTime + 0.05f, 0.016f);
        if (nav.HasDestination || nav.CurrentMovementAuthority != "stopped:actor_input") Fail("ActorControlBridge did not clear owned actor_input destination when source produced no frame");

        nav.SetDestination(npc.transform.position + Vector3.right * 5f, "npc_behavior");
        bridge.OUTL_Tick(world, world.WorldTime + 0.1f, 0.016f);
        if (!nav.HasDestination || nav.CurrentMovementAuthority != "npc_behavior") Fail("ActorControlBridge clear path stopped a non-owned npc_behavior destination");
    }

    private void SmokeInputDrivenNpcMaterializesAbstractTravel(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_InputMaterialize_NPC", world, new Vector3(20f, 0f, 14f));
        OUTL_NavMeshMover nav = npc.gameObject.AddComponent<OUTL_NavMeshMover>();
        nav.UseOUTLTick = false;
        OUTL_AIActor ai = npc.gameObject.AddComponent<OUTL_AIActor>();
        ai.Entity = npc;
        ai.Profile = ScriptableObject.CreateInstance<OUTL_AIProfile>();
        ai.UseActorInputContract = true;
        OUTL_BotInputDriver bot = npc.gameObject.AddComponent<OUTL_BotInputDriver>();
        bot.Entity = npc;
        bot.AIActor = ai;
        OUTL_ActorControlBridge bridge = npc.gameObject.AddComponent<OUTL_ActorControlBridge>();
        bridge.Entity = npc;
        bridge.InputSourceBehaviour = bot;
        OUTL_NPCBehaviorController behavior = npc.gameObject.AddComponent<OUTL_NPCBehaviorController>();
        behavior.Entity = npc;
        behavior.AIActor = ai;
        behavior.NavMover = nav;
        behavior.BotInputDriver = bot;
        behavior.ActorControlBridge = bridge;
        behavior.Model = ScriptableObject.CreateInstance<OUTL_NPCBehaviorModel>();
        behavior.Model.Schedule = ScriptableObject.CreateInstance<OUTL_NPCScheduleDef>();
        behavior.Model.NavigationProfile = ScriptableObject.CreateInstance<OUTL_NPCNavigationProfile>();
        behavior.Model.Schedule.Entries = new[] { new OUTL_NPCScheduleEntry { EntryId = "input_materialize", Action = OUTL_NPCScheduleActionType.TravelTo, TargetMode = OUTL_NPCScheduleTargetMode.FixedWorldPosition, TargetPosition = new Vector3(40f, 0f, 14f), StartTimeNormalized = 0f, EndTimeNormalized = 1f } };

        Vector3 abstractPosition = new Vector3(34f, 0f, 14f);
        npc.Runtime.Tier = OUTL_RuntimeTier.Full;
        behavior.Runtime.CurrentTier = OUTL_RuntimeTier.Full;
        behavior.Runtime.CurrentAction = OUTL_NPCScheduleActionType.TravelTo;
        behavior.Runtime.CurrentTargetPosition = new Vector3(40f, 0f, 14f);
        behavior.Runtime.AbstractPosition = abstractPosition;
        behavior.Runtime.Travel.Mode = OUTL_NPCTravelMode.Abstract;
        behavior.Runtime.Travel.AbstractPosition = abstractPosition;
        behavior.Runtime.Travel.TargetPosition = behavior.Runtime.CurrentTargetPosition;

        behavior.OUTL_Tick(world, world.WorldTime + 0.25f, 0.05f);
        if (Vector3.Distance(npc.transform.position, abstractPosition) > 0.25f) Fail("input-driven NPC did not materialize abstract position before exact actor input");
        if (nav.CurrentMovementAuthority == "npc_behavior") Fail("input-driven materialized NPC claimed direct npc_behavior movement");
    }

    private void SmokeInputDrivenNpcScheduleUsesActorInput(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_InputDrivenSchedule_NPC", world, new Vector3(0f, 0f, 15.5f));
        OUTL_NavMeshMover nav = npc.gameObject.AddComponent<OUTL_NavMeshMover>();
        nav.UseOUTLTick = false;
        OUTL_AIActor ai = npc.gameObject.AddComponent<OUTL_AIActor>();
        ai.Entity = npc;
        ai.Profile = ScriptableObject.CreateInstance<OUTL_AIProfile>();
        ai.UseActorInputContract = true;
        OUTL_BotInputDriver bot = npc.gameObject.AddComponent<OUTL_BotInputDriver>();
        bot.Entity = npc;
        bot.AIActor = ai;
        OUTL_ActorControlBridge bridge = npc.gameObject.AddComponent<OUTL_ActorControlBridge>();
        bridge.Entity = npc;
        bridge.InputSourceBehaviour = bot;
        OUTL_NavMoverInputSink sink = npc.gameObject.AddComponent<OUTL_NavMoverInputSink>();
        sink.Entity = npc;
        sink.NavMover = nav;
        sink.ZeroInputStopDelay = 0f;
        OUTL_NPCBehaviorController behavior = npc.gameObject.AddComponent<OUTL_NPCBehaviorController>();
        behavior.Entity = npc;
        behavior.AIActor = ai;
        behavior.NavMover = nav;
        behavior.BotInputDriver = bot;
        behavior.ActorControlBridge = bridge;
        behavior.Model = ScriptableObject.CreateInstance<OUTL_NPCBehaviorModel>();
        behavior.Model.Schedule = ScriptableObject.CreateInstance<OUTL_NPCScheduleDef>();
        behavior.Model.NavigationProfile = ScriptableObject.CreateInstance<OUTL_NPCNavigationProfile>();
        behavior.Model.Schedule.Entries = new[] { new OUTL_NPCScheduleEntry { EntryId = "input_travel", Action = OUTL_NPCScheduleActionType.TravelTo, TargetMode = OUTL_NPCScheduleTargetMode.FixedWorldPosition, TargetPosition = npc.transform.position + Vector3.forward * 6f, StartTimeNormalized = 0f, EndTimeNormalized = 1f } };
        npc.Runtime.Tier = OUTL_RuntimeTier.Full;

        behavior.OUTL_Tick(world, world.WorldTime, 0.05f);
        if (nav.CurrentMovementAuthority == "npc_behavior") Fail("input-driven NPCBehaviorController claimed NavMover directly");

        OUTL_ActorInputFrame frame = OUTL_ActorInputFrame.Empty(world.WorldTime);
        if (!bot.TryBuildInput(world, npc, world.WorldTime, 0.016f, ref frame)) Fail("BotInputDriver did not build schedule movement input");
        if (frame.Move.sqrMagnitude <= 0.0001f) Fail("schedule movement produced zero actor input");
        sink.OUTL_ApplyInput(frame, world);
        if (!nav.HasDestination || nav.CurrentMovementAuthority != "actor_input") Fail("schedule movement did not flow through actor_input authority");
    }

    private void SmokeStimulusInterrupt(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_Interrupt_NPC", world, new Vector3(0f, 0f, 16f));
        OUTL_NPCBehaviorController controller = npc.gameObject.AddComponent<OUTL_NPCBehaviorController>();
        controller.Entity = npc;
        controller.Model = ScriptableObject.CreateInstance<OUTL_NPCBehaviorModel>();
        controller.Model.Schedule = ScriptableObject.CreateInstance<OUTL_NPCScheduleDef>();
        controller.Model.Schedule.Entries = new[] { new OUTL_NPCScheduleEntry { EntryId = "idle", Action = OUTL_NPCScheduleActionType.Idle, StartTimeNormalized = 0f, EndTimeNormalized = 1f } };
        controller.Model.InterruptPolicies = new[] { new OUTL_NPCStimulusInterruptPolicy { StimulusTypes = new[] { OUTL_StimulusType.HeardNoise }, MinimumPriority = 0.1f, InterruptAction = OUTL_NPCScheduleActionType.Investigate, MaxDuration = 0.1f } };
        OUTL_StimulusBus.EmitSound(OUTL_EntityId.None, npc.transform.position, 10f, 1f, 1f, "smoke_noise");
        controller.OUTL_Tick(world, world.WorldTime, 0.25f);
        if (!controller.Runtime.HasActiveInterrupt || controller.Runtime.CurrentAction != OUTL_NPCScheduleActionType.Investigate) Fail("stimulus did not interrupt schedule");
        controller.OUTL_Tick(world, world.WorldTime + 1f, 1f);
        if (controller.Runtime.HasActiveInterrupt) Fail("interrupt did not complete/resume");
    }

    private void SmokeLootDropsOnce(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_Loot_NPC", world, new Vector3(0f, 0f, 24f));
        OUTL_LootDropper dropper = npc.gameObject.AddComponent<OUTL_LootDropper>();
        dropper.Entity = npc;
        dropper.DropOnlyOnce = true;
        bool first = dropper.Drop(npc.transform.position);
        bool second = dropper.Drop(npc.transform.position);
        if (second) Fail("loot dropper dropped twice");
        if (first && second) Fail("loot dropper inconsistent once guard");
    }

    private void SmokePickupAddsInventory(OUTL_World world)
    {
        OUTL_EntityAdapter receiver = CreateActor("Smoke_Pickup_Receiver", world, new Vector3(0f, 0f, 32f));
        OUTL_ItemDef item = ScriptableObject.CreateInstance<OUTL_ItemDef>();
        item.ClassName = "smoke_item";
        item.MaxStack = 99;

        GameObject go = CreateSmokeObject("Smoke_ItemPickup", new Vector3(1f, 0f, 32f));
        OUTL_EntityAdapter pickupEntity = go.AddComponent<OUTL_EntityAdapter>();
        pickupEntity.ClassNameOverride = "pickup_generic";
        pickupEntity.RegisterNow(world);
        OUTL_ItemPickup pickup = go.AddComponent<OUTL_ItemPickup>();
        pickup.Entity = pickupEntity;
        pickup.Item = item;
        pickup.Count = 3;

        if (!pickup.TryPickup(receiver)) Fail("pickup did not apply");
        if (world.Inventory.CountItem(receiver.Id, item) != 3) Fail("pickup did not add item count to inventory");
        if (!pickup.IsPickedUp) Fail("pickup did not mark pickedUp");
    }

    private void SmokeContainerOpensLootOnceAndPickupAddsInventory(OUTL_World world)
    {
        OUTL_EntityAdapter receiver = CreateActor("Smoke_Container_Receiver", world, new Vector3(0f, 0f, 36f));
        OUTL_InventoryRuntime inventory = receiver.gameObject.AddComponent<OUTL_InventoryRuntime>();
        inventory.Entity = receiver;

        OUTL_ItemDef item = ScriptableObject.CreateInstance<OUTL_ItemDef>();
        item.ClassName = "smoke_container_item";
        item.DisplayName = "Smoke Container Item";
        item.MaxStack = 99;
        inventory.KnownItems = new[] { item };

        GameObject pickupPrefab = CreateSmokePickupPrefab("Smoke_Container_PickupPrefab", item);
        OUTL_LootTableDef table = ScriptableObject.CreateInstance<OUTL_LootTableDef>();
        table.TableId = "smoke.container.loot";
        table.RollEachEntry = true;
        table.MaxDrops = 4;
        table.Entries = new[]
        {
            new OUTL_LootTableEntry
            {
                Label = "smoke.container.item",
                Item = item,
                PickupPrefab = pickupPrefab,
                Chance = 1f,
                Weight = 1f,
                MinCount = 2,
                MaxCount = 2,
                ScatterRadius = 0f,
                SpawnOffset = Vector3.up * 0.25f
            }
        };

        OUTL_ContainerDef def = ScriptableObject.CreateInstance<OUTL_ContainerDef>();
        def.ContainerId = "smoke.container";
        def.OpenKey = "smoke.container.open";
        def.LootKey = "smoke.container.loot";
        def.Seed = 3636;
        def.StartsLocked = false;
        def.LootTable = table;

        GameObject chestGo = CreateSmokeObject("Smoke_Container", new Vector3(1.5f, 0f, 36f));
        OUTL_EntityAdapter chest = chestGo.AddComponent<OUTL_EntityAdapter>();
        chest.ClassNameOverride = "container_smoke";
        chest.TargetName = "smoke.container";
        chest.StableId = "smoke.container";
        OUTL_ContainerRuntime container = chestGo.AddComponent<OUTL_ContainerRuntime>();
        container.Entity = chest;
        container.Def = def;
        container.RolledSeed = def.Seed;
        OUTL_ChestInteractable interactable = chestGo.AddComponent<OUTL_ChestInteractable>();
        interactable.Container = container;
        chest.RegisterNow(world);
        chest.RebuildCommandReceiverCache();

        bool sent = world.Commands.Send(new OUTL_Command(OUTL_CommandType.Use, receiver.Id, chest.Id) { Point = chest.transform.position });
        if (!sent) Fail("container use command was not handled");
        if (!container.IsOpen) Fail("container did not open");
        if (!container.Looted) Fail("container did not mark looted");
        if (container.LastSpawnedCount != 1) Fail("container did not spawn exactly one pickup stack");

        int spawnedBeforeSecondOpen = container.LastSpawnedCount;
        world.Commands.Send(new OUTL_Command(OUTL_CommandType.Open, receiver.Id, chest.Id) { Point = chest.transform.position });
        if (container.LastSpawnedCount != spawnedBeforeSecondOpen) Fail("container rerolled loot after already looted");

        OUTL_ItemPickup spawnedPickup = FindSmokePickup(world, item, "smoke.container.item");
        if (spawnedPickup == null) Fail("container spawned no pickup with configured item");
        else
        {
            if (spawnedPickup.Count != 2) Fail("container pickup stack count did not honor MinCount/MaxCount");
            if (!spawnedPickup.TryPickup(receiver)) Fail("container pickup could not be picked up");
            if (world.Inventory.CountItem(receiver.Id, item) != 2) Fail("container pickup did not add item to inventory");
        }

        if (!chest.Runtime.State.GetFlag("ContainerOpen")) Fail("container runtime state missing ContainerOpen");
        if (!chest.Runtime.State.GetFlag("ContainerLooted")) Fail("container runtime state missing ContainerLooted");
    }

    private void SmokeDoorButtonLogicChain(OUTL_World world)
    {
        OUTL_EntityAdapter source = CreateActor("Smoke_Logic_Source", world, new Vector3(0f, 0f, 38f));

        GameObject doorGo = CreateSmokeObject("Smoke_Logic_Door", new Vector3(0f, 1f, 39f));
        OUTL_EntityAdapter doorEntity = doorGo.AddComponent<OUTL_EntityAdapter>();
        doorEntity.ClassNameOverride = "logic_door";
        doorEntity.TargetName = "smoke.logic.door";
        doorEntity.StableId = "smoke.logic.door";
        OUTL_Door door = doorGo.AddComponent<OUTL_Door>();
        door.Entity = doorEntity;
        door.DoorRoot = doorGo.transform;
        door.AutoClose = false;
        door.ToggleMode = false;
        door.OpenLocalPosition = doorGo.transform.localPosition + Vector3.up * 2f;
        doorEntity.RegisterNow(world);
        doorEntity.RebuildCommandReceiverCache();

        GameObject gateGo = CreateSmokeObject("Smoke_Logic_AND_Gate", new Vector3(0f, 0f, 38.5f));
        OUTL_EntityAdapter gateEntity = gateGo.AddComponent<OUTL_EntityAdapter>();
        gateEntity.ClassNameOverride = "logic_gate";
        gateEntity.TargetName = "smoke.logic.gate";
        gateEntity.StableId = "smoke.logic.gate";
        OUTL_LogicGate gate = gateGo.AddComponent<OUTL_LogicGate>();
        gate.Entity = gateEntity;
        gate.Mode = OUTL_BooleanGateMode.And;
        gate.InputCount = 2;
        gate.Inputs = new bool[2];
        gate.Outputs = new[]
        {
            new OUTL_OutputLink { EventName = "OnTrue", TargetName = "smoke.logic.door", Command = OUTL_CommandType.Open },
            new OUTL_OutputLink { EventName = "OnFalse", TargetName = "smoke.logic.door", Command = OUTL_CommandType.Close }
        };
        gateEntity.RegisterNow(world);
        gateEntity.RebuildCommandReceiverCache();

        OUTL_EntityAdapter buttonA = CreateLogicSmokeButton(world, "Smoke_Logic_Button_A", "smoke.logic.button.a", "smoke.logic.gate", 0, new Vector3(-1f, 0f, 38f));
        OUTL_EntityAdapter buttonB = CreateLogicSmokeButton(world, "Smoke_Logic_Button_B", "smoke.logic.button.b", "smoke.logic.gate", 1, new Vector3(1f, 0f, 38f));

        if (!world.Commands.Send(new OUTL_Command(OUTL_CommandType.Use, source.Id, buttonA.Id) { Point = buttonA.transform.position })) Fail("logic button A did not handle use");
        if (door.IsOpen) Fail("logic door opened before AND gate was complete");
        if (!world.Commands.Send(new OUTL_Command(OUTL_CommandType.Use, source.Id, buttonB.Id) { Point = buttonB.transform.position })) Fail("logic button B did not handle use");
        if (!gate.Output || !door.IsOpen) Fail("logic AND gate did not open door after both inputs");
        if (!world.Commands.Send(new OUTL_Command(OUTL_CommandType.Use, source.Id, buttonA.Id) { Point = buttonA.transform.position })) Fail("logic button A release did not handle use");
        if (gate.Output || door.IsOpen) Fail("logic AND gate did not close door when an input released");
    }

    private void SmokeAccessKeyDoor(OUTL_World world)
    {
        OUTL_EntityAdapter source = CreateActor("Smoke_Access_Source", world, new Vector3(0f, 0f, 42f));
        OUTL_ItemDef key = ScriptableObject.CreateInstance<OUTL_ItemDef>();
        key.ClassName = "smoke.access.red_key";
        key.DisplayName = "Smoke Red Key";
        key.MaxStack = 99;

        OUTL_InventoryRuntime inventory = source.gameObject.AddComponent<OUTL_InventoryRuntime>();
        inventory.Entity = source;
        OUTL_DefDatabase database = ScriptableObject.CreateInstance<OUTL_DefDatabase>();
        database.ItemDefs = new[] { key };
        database.Rebuild();
        inventory.DefDatabase = database;
        inventory.KnownItems = new OUTL_ItemDef[0];

        world.Inventory.AddItem(source.Id, key, 2);
        if (world.Inventory.TryConsume(source.Id, key, 3)) Fail("inventory atomically consumed an unavailable key count");
        if (world.Inventory.CountItem(source.Id, key) != 2) Fail("failed inventory consume removed a partial key stack");
        world.Inventory.Clear(source.Id);

        GameObject doorGo = CreateSmokeObject("Smoke_Access_Door", new Vector3(0f, 1f, 43f));
        OUTL_EntityAdapter doorEntity = doorGo.AddComponent<OUTL_EntityAdapter>();
        doorEntity.ClassNameOverride = "access_door";
        doorEntity.TargetName = "smoke.access.door";
        doorEntity.StableId = "smoke.access.door";
        OUTL_Door door = doorGo.AddComponent<OUTL_Door>();
        door.Entity = doorEntity;
        door.DoorRoot = doorGo.transform;
        door.AutoClose = false;
        door.ToggleMode = false;
        door.OpenLocalPosition = doorGo.transform.localPosition + Vector3.up * 2f;
        OUTL_AccessController access = doorGo.AddComponent<OUTL_AccessController>();
        access.Entity = doorEntity;
        access.StartsLocked = true;
        access.IsLocked = true;
        access.UnlockPermanentlyOnGrant = true;
        access.ConsumePolicy = OUTL_AccessConsumePolicy.Never;
        access.Requirements = new[]
        {
            new OUTL_AccessRequirement
            {
                RequirementId = "smoke.red_key",
                Condition = new OUTL_ConditionDef
                {
                    Op = OUTL_ConditionOp.HasItem,
                    Subject = OUTL_ConditionSubject.Source,
                    ItemDef = key,
                    IntValue = 1
                }
            }
        };
        doorEntity.RegisterNow(world);
        doorEntity.RebuildCommandReceiverCache();
        doorEntity.RebuildCommandGuardCache();

        bool denied = world.Commands.Send(new OUTL_Command(OUTL_CommandType.Use, source.Id, doorEntity.Id) { Point = doorGo.transform.position });
        if (denied || door.IsOpen || !access.IsLocked) Fail("locked access door accepted a user without a key");
        if (access.LastDeniedRequirement != "smoke.red_key") Fail("access denial did not expose the failed requirement");

        world.Inventory.AddItem(source.Id, key, 1);
        bool granted = world.Commands.Send(new OUTL_Command(OUTL_CommandType.Use, source.Id, doorEntity.Id) { Point = doorGo.transform.position });
        if (!granted || !door.IsOpen || access.IsLocked) Fail("key did not authorize and unlock the access door");
        if (world.Inventory.CountItem(source.Id, key) != 1) Fail("Doom-style retained key was consumed");

        OUTL_ComponentSavePayload inventoryPayload = OUTL_ComponentSaveUtility.Capture(inventory);
        world.Inventory.Clear(source.Id);
        inventory.OUTL_Restore(new OUTL_ComponentSaveReader(inventoryPayload));
        if (world.Inventory.CountItem(source.Id, key) != 1) Fail("stable-id inventory restore failed without KnownItems");

        world.Commands.Send(new OUTL_Command(OUTL_CommandType.Lock, source.Id, doorEntity.Id));
        access.UnlockPermanentlyOnGrant = false;
        access.ConsumePolicy = OUTL_AccessConsumePolicy.EveryGrant;
        access.Requirements[0].ConsumeItem = true;
        bool consumedGrant = world.Commands.Send(new OUTL_Command(OUTL_CommandType.Use, source.Id, doorEntity.Id));
        if (!consumedGrant || world.Inventory.CountItem(source.Id, key) != 0) Fail("consumable access key was not atomically consumed");
    }

    private void SmokeLogicSaveRoundtrip(OUTL_World world)
    {
        GameObject doorGo = CreateSmokeObject("Smoke_Save_Logic_Door", new Vector3(0f, 0f, 45f));
        OUTL_EntityAdapter entity = doorGo.AddComponent<OUTL_EntityAdapter>();
        entity.ClassNameOverride = "save_logic";
        entity.StableId = "smoke.save.logic";
        OUTL_Door door = doorGo.AddComponent<OUTL_Door>();
        door.Entity = entity;
        door.DoorRoot = doorGo.transform;
        entity.RegisterNow(world);
        door.IsOpen = true;
        door.Moving = true;
        doorGo.transform.localPosition = new Vector3(3f, 4f, 5f);
        OUTL_ComponentSavePayload doorPayload = OUTL_ComponentSaveUtility.Capture(door);
        door.IsOpen = false;
        door.Moving = false;
        doorGo.transform.localPosition = Vector3.zero;
        door.OUTL_Restore(new OUTL_ComponentSaveReader(doorPayload));
        if (!door.IsOpen || !door.Moving || (doorGo.transform.localPosition - new Vector3(3f, 4f, 5f)).sqrMagnitude > 0.0001f)
            Fail("door component save did not restore logical and visual state");

        OUTL_Button button = doorGo.AddComponent<OUTL_Button>();
        button.Entity = entity;
        button.State = true;
        button.Outputs = new[] { new OUTL_OutputLink { EventName = "OnPressed", TargetName = "unused", Command = OUTL_CommandType.Activate, Once = true, Fired = true } };
        OUTL_ComponentSavePayload buttonPayload = OUTL_ComponentSaveUtility.Capture(button);
        button.State = false;
        button.Outputs[0].Fired = false;
        button.OUTL_Restore(new OUTL_ComponentSaveReader(buttonPayload));
        if (!button.State || !button.Outputs[0].Fired) Fail("button save did not restore state/once output");

        OUTL_LogicGate gate = doorGo.AddComponent<OUTL_LogicGate>();
        gate.Entity = entity;
        gate.InputCount = 3;
        gate.Inputs = new[] { true, false, true };
        gate.Output = true;
        OUTL_ComponentSavePayload gatePayload = OUTL_ComponentSaveUtility.Capture(gate);
        gate.Inputs = new bool[1];
        gate.Output = false;
        gate.OUTL_Restore(new OUTL_ComponentSaveReader(gatePayload));
        if (!gate.Output || gate.Inputs.Length != 3 || !gate.Inputs[0] || gate.Inputs[1] || !gate.Inputs[2])
            Fail("logic gate save did not restore indexed inputs");
    }

    private void SmokeDeadNpcStopsSystems(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_DeadStops_NPC", world, new Vector3(0f, 0f, 40f));
        OUTL_AttackDriver attack = npc.gameObject.AddComponent<OUTL_AttackDriver>();
        attack.Source = npc;
        OUTL_NavMeshMover nav = npc.gameObject.AddComponent<OUTL_NavMeshMover>();
        nav.UseOUTLTick = false;
        nav.SetDestination(npc.transform.position + Vector3.forward * 8f, "smoke");
        OUTL_NPCBehaviorController behavior = npc.gameObject.AddComponent<OUTL_NPCBehaviorController>();
        behavior.Entity = npc;
        behavior.NavMover = nav;
        behavior.AttackDriver = attack;
        behavior.Model = ScriptableObject.CreateInstance<OUTL_NPCBehaviorModel>();
        behavior.Model.Schedule = ScriptableObject.CreateInstance<OUTL_NPCScheduleDef>();
        behavior.Model.Schedule.Entries = new[] { new OUTL_NPCScheduleEntry { EntryId = "travel", Action = OUTL_NPCScheduleActionType.TravelTo, TargetMode = OUTL_NPCScheduleTargetMode.FixedWorldPosition, TargetPosition = npc.transform.position + Vector3.forward * 6f, StartTimeNormalized = 0f, EndTimeNormalized = 1f } };
        OUTL_DeathHandler handler = npc.gameObject.AddComponent<OUTL_DeathHandler>();
        handler.Entity = npc;
        handler.QueueDespawn = false;

        OUTL_Combat.ApplyDamage(OUTL_EntityId.None, npc.Id, 200f, npc.transform.position, "smoke_dead_stop");
        world.Events.Flush();
        if (!attack.BlockedByVitals) Fail("dead NPC attack driver not blocked");
        if (nav.HasDestination) Fail("dead NPC nav still has destination");
        if (behavior.Runtime.CurrentEntryId != "dead") Fail("dead NPC behavior did not enter dead state");
    }

    private void SmokeSaveLoadRestoresNpcRuntime(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_SaveTravel_NPC", world, new Vector3(0f, 0f, 48f));
        OUTL_NPCBehaviorController controller = npc.gameObject.AddComponent<OUTL_NPCBehaviorController>();
        controller.Entity = npc;
        controller.Model = ScriptableObject.CreateInstance<OUTL_NPCBehaviorModel>();
        controller.Model.Schedule = ScriptableObject.CreateInstance<OUTL_NPCScheduleDef>();
        controller.Model.NavigationProfile = ScriptableObject.CreateInstance<OUTL_NPCNavigationProfile>();
        controller.Model.Schedule.ScheduleId = "smoke_schedule";
        controller.Model.Schedule.Entries = new[] { new OUTL_NPCScheduleEntry { EntryId = "save_travel", Action = OUTL_NPCScheduleActionType.TravelTo, TargetMode = OUTL_NPCScheduleTargetMode.FixedWorldPosition, TargetPosition = new Vector3(10f, 0f, 48f), StartTimeNormalized = 0f, EndTimeNormalized = 1f } };
        npc.Runtime.Tier = OUTL_RuntimeTier.Far;
        controller.OUTL_Tick(world, world.WorldTime + 1f, 1f);
        OUTL_WorldSaveFile file = world.Save.Capture();

        controller.Runtime.CurrentEntryId = "mutated";
        controller.Runtime.RouteProgress = 0f;
        controller.Runtime.Travel.RouteProgress = 0f;
        npc.Runtime.Tier = OUTL_RuntimeTier.Full;
        if (!world.Save.Restore(file)) Fail("save restore returned false for NPC runtime");
        if (controller.Runtime.CurrentEntryId != "save_travel") Fail("save/load did not restore NPC schedule entry");
        if (controller.Runtime.RouteProgress <= 0f && controller.Runtime.Travel.RouteProgress <= 0f) Fail("save/load did not restore NPC route progress");
        if (npc.Runtime.Tier != OUTL_RuntimeTier.Far) Fail("save/load did not restore NPC tier");
    }

    private void SmokeSaveLoadRestoresDeathAndInventory(OUTL_World world)
    {
        OUTL_EntityAdapter npc = CreateActor("Smoke_SaveDeathInventory_NPC", world, new Vector3(0f, 0f, 56f));
        OUTL_ItemDef item = ScriptableObject.CreateInstance<OUTL_ItemDef>();
        item.ClassName = "smoke_saved_item";
        item.MaxStack = 99;
        OUTL_InventoryRuntime inventory = npc.gameObject.AddComponent<OUTL_InventoryRuntime>();
        inventory.Entity = npc;
        inventory.KnownItems = new[] { item };
        world.Inventory.AddItem(npc.Id, item, 7);
        OUTL_Combat.ApplyDamage(OUTL_EntityId.None, npc.Id, 200f, npc.transform.position, "smoke_save_dead");
        OUTL_WorldSaveFile file = world.Save.Capture();

        npc.Runtime.Dead = false;
        npc.Runtime.LifeState = OUTL_LifeState.Alive;
        npc.Runtime.State.SetFlag(OUTL_StateId.Dead, false);
        npc.Runtime.Stats.Set(OUTL_StatId.Health, 100f);
        world.Inventory.Clear(npc.Id);

        if (!world.Save.Restore(file)) Fail("save restore returned false for death/inventory");
        if (!npc.Runtime.Dead || npc.Runtime.LifeState != OUTL_LifeState.Dead) Fail("save/load did not restore dead runtime");
        if (world.Inventory.CountItem(npc.Id, item) != 7) Fail("save/load did not restore inventory count");
    }

    private OUTL_World EnsureWorld()
    {
        if (OUTL_World.Instance != null) return OUTL_World.Instance;
        GameObject go = CreateSmokeObject("Smoke_World", Vector3.zero);
        return go.AddComponent<OUTL_World>();
    }

    private OUTL_EntityAdapter CreateActor(string name, OUTL_World world, Vector3 position)
    {
        GameObject go = CreateSmokeObject(name, position);
        OUTL_EntityAdapter entity = go.AddComponent<OUTL_EntityAdapter>();
        entity.ClassNameOverride = "actor_generic";
        entity.RegisterNow(world);
        OUTL_Vitals vitals = go.AddComponent<OUTL_Vitals>();
        vitals.Entity = entity;
        vitals.DefaultHealth = 100f;
        vitals.DefaultMaxHealth = 100f;
        go.AddComponent<OUTL_DamageReceiver>().Entity = entity;
        go.AddComponent<OUTL_DeathRuntime>().Entity = entity;
        vitals.EnsureInitialized();
        return entity;
    }

    private GameObject CreateSmokePickupPrefab(string name, OUTL_ItemDef item)
    {
        GameObject go = CreateSmokeObject(name, Vector3.zero);
        go.SetActive(false);
        OUTL_EntityAdapter entity = go.AddComponent<OUTL_EntityAdapter>();
        entity.ClassNameOverride = "pickup_smoke";
        entity.TargetName = name;
        entity.StableId = name;
        entity.RegisterOnEnable = true;
        OUTL_ItemPickup pickup = go.AddComponent<OUTL_ItemPickup>();
        pickup.Entity = entity;
        pickup.Item = item;
        pickup.Count = 1;
        pickup.AutoDespawnOnPickup = true;
        pickup.PickupKey = "smoke.container.item";
        return go;
    }

    private OUTL_EntityAdapter CreateLogicSmokeButton(OUTL_World world, string name, string targetName, string gateTargetName, int inputIndex, Vector3 position)
    {
        GameObject go = CreateSmokeObject(name, position);
        OUTL_EntityAdapter entity = go.AddComponent<OUTL_EntityAdapter>();
        entity.ClassNameOverride = "logic_button";
        entity.TargetName = targetName;
        entity.StableId = targetName;
        OUTL_Button button = go.AddComponent<OUTL_Button>();
        button.Entity = entity;
        button.Toggle = true;
        button.OverrideOutputIntWithState = false;
        button.Outputs = new[]
        {
            new OUTL_OutputLink { EventName = "OnPressed", TargetName = gateTargetName, Command = OUTL_CommandType.Activate, IntValue = inputIndex },
            new OUTL_OutputLink { EventName = "OnReleased", TargetName = gateTargetName, Command = OUTL_CommandType.Deactivate, IntValue = inputIndex }
        };
        entity.RegisterNow(world);
        entity.RebuildCommandReceiverCache();
        return entity;
    }

    private static OUTL_ItemPickup FindSmokePickup(OUTL_World world, OUTL_ItemDef item, string pickupKey)
    {
        if (world == null || item == null) return null;
        List<OUTL_EntityRuntime> entities = new List<OUTL_EntityRuntime>(32);
        world.Registry.CopyAll(entities);
        for (int i = 0; i < entities.Count; i++)
        {
            OUTL_EntityRuntime runtime = entities[i];
            if (runtime == null || runtime.Adapter == null) continue;
            OUTL_ItemPickup pickup = runtime.Adapter.GetComponent<OUTL_ItemPickup>();
            if (pickup == null || pickup.Item != item || pickup.IsPickedUp) continue;
            if (!string.IsNullOrEmpty(pickupKey) && pickup.PickupKey != pickupKey) continue;
            return pickup;
        }
        return null;
    }

    private GameObject CreateSmokeObject(string name, Vector3 position)
    {
        GameObject go = new GameObject("OUTL_" + name);
        go.transform.position = position;
        createdObjects.Add(go);
        return go;
    }

    private static void DestroySmokeObject(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(go);
        else UnityEngine.Object.DestroyImmediate(go);
    }

    private void Fail(string message)
    {
        failures++;
        Debug.LogError("[OUTL NPC World Smoke] " + message, this);
    }
}
