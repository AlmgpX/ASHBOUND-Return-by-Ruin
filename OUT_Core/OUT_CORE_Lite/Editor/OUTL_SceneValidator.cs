#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class OUTL_SceneValidator
{
    private const string MenuPath = "OUT CORE Lite/Diagnostics/Validate Open Scene";
    private const string GroundingMenuPath = "OUT CORE Lite/Advanced/Diagnostics/Validate Physics Grounding";

    [MenuItem(MenuPath)]
    public static void ValidateOpenScene()
    {
        OUTL_World[] worlds = Object.FindObjectsOfType<OUTL_World>(true);
        OUTL_EntityAdapter[] entities = Object.FindObjectsOfType<OUTL_EntityAdapter>(true);
        OUTL_TouchTrigger[] touchTriggers = Object.FindObjectsOfType<OUTL_TouchTrigger>(true);
        OUTL_Interactable[] interactables = Object.FindObjectsOfType<OUTL_Interactable>(true);
        OUTL_AIActor[] aiActors = Object.FindObjectsOfType<OUTL_AIActor>(true);
        OUTL_AttackDriver[] attackDrivers = Object.FindObjectsOfType<OUTL_AttackDriver>(true);
        OUTL_ActorControlBridge[] controlBridges = Object.FindObjectsOfType<OUTL_ActorControlBridge>(true);
        OUTL_NPCBehaviorController[] npcControllers = Object.FindObjectsOfType<OUTL_NPCBehaviorController>(true);
        OUTL_ChunkProcessingDriver[] chunkDrivers = Object.FindObjectsOfType<OUTL_ChunkProcessingDriver>(true);
        OUTL_AccessController[] accessControllers = Object.FindObjectsOfType<OUTL_AccessController>(true);
        OUTL_InventoryRuntime[] inventories = Object.FindObjectsOfType<OUTL_InventoryRuntime>(true);

        Dictionary<string, OUTL_EntityAdapter> stableIds = new Dictionary<string, OUTL_EntityAdapter>(128);
        Dictionary<string, OUTL_EntityAdapter> firstTargetNameOwner = new Dictionary<string, OUTL_EntityAdapter>(128);
        HashSet<string> duplicateTargetNames = new HashSet<string>();
        HashSet<string> targetNames = new HashSet<string>();
        int warnings = 0;
        int errors = 0;

        if (worlds.Length == 0)
        {
            LogError("scene has no OUTL_World", null);
            errors++;
        }
        else if (worlds.Length > 1)
        {
            for (int i = 0; i < worlds.Length; i++) LogError("multiple OUTL_World instances in scene", worlds[i]);
            errors += worlds.Length;
        }
        else
        {
            warnings += ValidateRuntimeRoot(worlds[0]);
        }

        for (int i = 0; i < entities.Length; i++)
        {
            OUTL_EntityAdapter e = entities[i];
            if (e == null) continue;

            warnings += ValidateAddressing(e, targetNames, firstTargetNameOwner, duplicateTargetNames);
            warnings += ValidateEntityComponentConflicts(e);
            warnings += ValidateActorCombatStack(e);
            warnings += ValidateNewActorInputStack(e);

            if (!string.IsNullOrEmpty(e.StableId))
            {
                OUTL_EntityAdapter existing;
                if (stableIds.TryGetValue(e.StableId, out existing) && existing != e)
                {
                    LogError("duplicate StableId '" + e.StableId + "'", e);
                    errors++;
                }
                else stableIds[e.StableId] = e;
            }

            if (e.SavePersistent && string.IsNullOrEmpty(e.StableId))
            {
                LogWarning("persistent entity without StableId", e);
                warnings++;
            }

            if (e.Def == null && string.IsNullOrEmpty(e.ClassNameOverride))
            {
                LogWarning("EntityAdapter without Def and without ClassNameOverride", e);
                warnings++;
            }
        }

        foreach (string duplicate in duplicateTargetNames)
        {
            LogWarning("duplicate TargetName '" + duplicate + "'. This is allowed for multicast-style addressing, but check that it is intentional.", firstTargetNameOwner.ContainsKey(duplicate) ? firstTargetNameOwner[duplicate] : null);
            warnings++;
        }

        for (int i = 0; i < entities.Length; i++)
        {
            OUTL_EntityAdapter e = entities[i];
            if (e == null) continue;
            warnings += ValidateOutgoingEntityAddress("EntityAdapter.Target", e.Target, targetNames, e);
            warnings += ValidateOutgoingEntityAddress("EntityAdapter.KillTarget", e.KillTarget, targetNames, e);
        }

        for (int i = 0; i < touchTriggers.Length; i++)
        {
            OUTL_TouchTrigger t = touchTriggers[i];
            if (t == null) continue;
            Collider c = t.GetComponent<Collider>();
            if (c == null || !c.isTrigger)
            {
                LogWarning("TouchTrigger without trigger collider", t);
                warnings++;
            }
            warnings += ValidateOutputs("TouchTrigger", t.Outputs, targetNames, t);
        }

        for (int i = 0; i < interactables.Length; i++)
        {
            OUTL_Interactable interactable = interactables[i];
            if (interactable == null) continue;
            warnings += ValidateInteractable(interactable, targetNames);
        }

        for (int i = 0; i < accessControllers.Length; i++)
        {
            OUTL_AccessController access = accessControllers[i];
            if (access == null) continue;
            warnings += ValidateAccessController(access, targetNames);
        }

        for (int i = 0; i < inventories.Length; i++)
        {
            OUTL_InventoryRuntime inventory = inventories[i];
            if (inventory == null) continue;
            if (inventory.DefDatabase == null && (inventory.KnownItems == null || inventory.KnownItems.Length == 0))
            {
                LogWarning("InventoryRuntime has neither DefDatabase nor KnownItems. Dynamic inventory items cannot be resolved on restore.", inventory);
                warnings++;
            }
        }

        for (int i = 0; i < controlBridges.Length; i++)
        {
            OUTL_ActorControlBridge bridge = controlBridges[i];
            if (bridge == null) continue;
            warnings += ValidateActorControlBridge(bridge);
        }

        for (int i = 0; i < aiActors.Length; i++)
        {
            OUTL_AIActor ai = aiActors[i];
            if (ai == null) continue;
            warnings += ValidateAICombatStack(ai);
        }

        for (int i = 0; i < npcControllers.Length; i++)
        {
            OUTL_NPCBehaviorController npc = npcControllers[i];
            if (npc == null) continue;
            warnings += ValidateNPCBehaviorStack(npc);
        }

        for (int i = 0; i < attackDrivers.Length; i++)
        {
            OUTL_AttackDriver attack = attackDrivers[i];
            if (attack == null) continue;
            warnings += ValidateAttackDriverStack(attack);
        }

        warnings += ValidateParallelReadiness(chunkDrivers, aiActors);
        warnings += ValidateSectorIntegritySummary(worlds);
        warnings += ValidatePhysicsGroundingInternal(false);
        warnings += ValidateRuntimeConstructionSource();

        Debug.Log("OUT CORE Lite scene validation complete. errors=" + errors + " warnings=" + warnings + " entities=" + entities.Length + " targetNames=" + targetNames.Count + ". Canonical actor stack is ActorInputFrame -> ActorControlBridge -> phased input sinks.");
    }

    // [MenuItem(GroundingMenuPath)]
    public static void ValidatePhysicsGrounding()
    {
        int warnings = ValidatePhysicsGroundingInternal(true);
        Debug.Log("OUT CORE Lite physics grounding validation complete. warnings=" + warnings + ".");
    }

    private static int ValidateRuntimeRoot(OUTL_World world)
    {
        int warnings = 0;
        if (world == null) return 0;
        GameObject root = world.gameObject;
        if (root.GetComponent<OUTL_PoolSystem>() == null) { LogWarning("Runtime root missing OUTL_PoolSystem. Add it or use Foundation/Workbench setup.", root); warnings++; }
        if (root.GetComponent<OUTL_SaveSpawnResolverRegistry>() == null) { LogWarning("Runtime root missing OUTL_SaveSpawnResolverRegistry. Save/load spawn restore may be incomplete.", root); warnings++; }
        if (root.GetComponent<OUTL_GameLoopRunner>() == null) { LogWarning("Runtime root missing OUTL_GameLoopRunner. OUTL scheduler/game loop may not tick in play mode.", root); warnings++; }
        if (root.GetComponent<OUTL_ChunkProcessingDriver>() == null) { LogWarning("Runtime root has no OUTL_ChunkProcessingDriver. This is allowed for tiny tests, but large worlds need runtime tier/sector processing.", root); warnings++; }
        return warnings;
    }

    private static int ValidateActorCombatStack(OUTL_EntityAdapter entity)
    {
        int warnings = 0;
        if (entity == null) return 0;

        GameObject go = entity.gameObject;
        bool hasReceiver = go.GetComponent<OUTL_DamageReceiver>() != null;
        bool hasVitals = go.GetComponent<OUTL_Vitals>() != null;
        bool hasDeathHandler = go.GetComponent<OUTL_DeathHandler>() != null;
        bool hasDeathRuntime = go.GetComponent<OUTL_DeathRuntime>() != null;
        bool hasHitbox = go.GetComponentInChildren<OUTL_Hitbox>(true) != null;
        bool hasAI = go.GetComponent<OUTL_AIActor>() != null;
        bool hasAttack = go.GetComponent<OUTL_AttackDriver>() != null;
        bool hasHealthPath = HasHealthDefaultPath(entity);
        Collider rootCollider = go.GetComponent<Collider>();
        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        bool hasCollider = colliders != null && colliders.Length > 0;

        bool actorCandidate = hasReceiver || hasVitals || hasDeathHandler || hasDeathRuntime || hasHitbox || hasAI || hasAttack || hasHealthPath || HasActorTag(entity);
        if (!actorCandidate) return 0;

        if (!hasReceiver) { LogWarning("damageable actor missing OUTL_DamageReceiver.", entity); warnings++; }
        if (!hasVitals) { LogWarning("damageable actor missing OUTL_Vitals.", entity); warnings++; }
        if (!hasDeathHandler && !hasDeathRuntime) { LogWarning("killable actor missing death component. Add OUTL_DeathRuntime and/or OUTL_DeathHandler.", entity); warnings++; }
        if (!hasCollider) { LogWarning("actor has combat stack but no Collider. Hitscan/projectile/melee cannot resolve damage.", entity); warnings++; }
        if (hasCollider && !hasHitbox && rootCollider == null)
        {
            LogWarning("actor has child colliders but no OUTL_Hitbox and no root collider fallback. Add a generic/root hitbox or root collider.", entity);
            warnings++;
        }
        if (hasVitals && !hasHealthPath)
        {
            LogWarning("actor with OUTL_Vitals has no Health/MaxHealth default path. Add BaseStats on Def or enable OUTL_Vitals InitializeMissingStats.", entity);
            warnings++;
        }
        if (entity.Def != null && entity.Def.Prefab != null && entity.Def.Prefab.GetComponent<OUTL_EntityAdapter>() == null)
        {
            LogWarning("pooled actor EntityDef prefab is missing OUTL_EntityAdapter: " + entity.Def.Prefab.name, entity.Def.Prefab);
            warnings++;
        }

        OUTL_Hitbox[] hitboxes = go.GetComponentsInChildren<OUTL_Hitbox>(true);
        for (int i = 0; i < hitboxes.Length; i++)
        {
            OUTL_Hitbox hitbox = hitboxes[i];
            if (hitbox == null) continue;
            if (hitbox.Entity == null) { LogWarning("OUTL_Hitbox without Entity reference; repair can bind it to the actor root.", hitbox); warnings++; }
            if (hitbox.GetComponent<Collider>() == null) { LogWarning("OUTL_Hitbox should sit on a collider GameObject for precise zone/multiplier damage.", hitbox); warnings++; }
        }

        return warnings;
    }

    private static int ValidateNewActorInputStack(OUTL_EntityAdapter entity)
    {
        int warnings = 0;
        if (entity == null) return 0;
        GameObject go = entity.gameObject;

        OUTL_PlayerInputSource playerInput = go.GetComponent<OUTL_PlayerInputSource>();
        OUTL_BotInputDriver botInput = go.GetComponent<OUTL_BotInputDriver>();
        OUTL_ActorControlBridge bridge = go.GetComponent<OUTL_ActorControlBridge>();
        bool hasAnyInputSource = playerInput != null || botInput != null;
        bool actorLike = HasActorTag(entity) || hasAnyInputSource || go.GetComponent<OUTL_AIActor>() != null || go.GetComponent<OUTL_AttackDriver>() != null;
        if (!actorLike) return 0;

        if (hasAnyInputSource && bridge == null)
        {
            LogWarning("actor has PlayerInputSource/BotInputDriver but no OUTL_ActorControlBridge. New stack is InputSource -> ActorControlBridge -> phased sinks.", entity);
            warnings++;
        }

        if (bridge != null)
        {
            warnings += ValidateActorControlBridge(bridge);
        }

        if (playerInput != null)
        {
            if (go.GetComponent<OUTL_CharacterControllerInputSink>() == null && go.GetComponent<OUTL_NavMoverInputSink>() == null)
            {
                LogWarning("player actor uses OUTL_PlayerInputSource but has no movement input sink. Add OUTL_CharacterControllerInputSink for prototype player control.", playerInput);
                warnings++;
            }
            if (playerInput.ViewCamera == null && go.GetComponentInChildren<Camera>(true) == null)
            {
                LogWarning("player actor has no ViewCamera/camera child for aim/look input.", playerInput);
                warnings++;
            }
        }

        if (botInput != null)
        {
            OUTL_AIActor ai = go.GetComponent<OUTL_AIActor>();
            if (ai != null && !ai.UseActorInputContract)
            {
                LogWarning("AI actor has BotInputDriver but OUTL_AIActor.UseActorInputContract is disabled. This creates split direct-AI/input control and can cause jitter.", ai);
                warnings++;
            }
            if (go.GetComponent<OUTL_TacticalPlanner>() == null && go.GetComponent<OUTL_NPCBehaviorController>() == null)
            {
                LogWarning("AI-controlled actor has BotInputDriver but no TacticalPlanner or NPCBehaviorController.", botInput);
                warnings++;
            }
            if (go.GetComponent<OUTL_AimPlanner>() == null && go.GetComponent<OUTL_AttackDriver>() != null)
            {
                LogWarning("armed AI actor has BotInputDriver + AttackDriver but no OUTL_AimPlanner. Aim/fire authorization will be weak.", botInput);
                warnings++;
            }
        }

        return warnings;
    }

    private static int ValidateActorControlBridge(OUTL_ActorControlBridge bridge)
    {
        int warnings = 0;
        if (bridge == null) return 0;
        if (bridge.Entity == null && bridge.GetComponent<OUTL_EntityAdapter>() == null) { LogWarning("ActorControlBridge has no Entity/EntityAdapter.", bridge); warnings++; }
        if (bridge.InputSourceBehaviour == null) { LogWarning("ActorControlBridge has no InputSourceBehaviour.", bridge); warnings++; }
        else if (!(bridge.InputSourceBehaviour is OUTL_IActorInputSource)) { LogWarning("ActorControlBridge InputSourceBehaviour does not implement OUTL_IActorInputSource.", bridge.InputSourceBehaviour); warnings++; }
        if (bridge.InputSinkBehaviours == null || bridge.InputSinkBehaviours.Length == 0) { LogWarning("ActorControlBridge has no InputSinkBehaviours.", bridge); warnings++; }
        else
        {
            int lastPhase = int.MinValue;
            bool hasMovement = false;
            bool hasWeapon = false;
            for (int i = 0; i < bridge.InputSinkBehaviours.Length; i++)
            {
                Behaviour behaviour = bridge.InputSinkBehaviours[i];
                if (behaviour == null) { LogWarning("ActorControlBridge has null input sink at index " + i + ".", bridge); warnings++; continue; }
                OUTL_IActorInputSink sink = behaviour as OUTL_IActorInputSink;
                if (sink == null) { LogWarning("ActorControlBridge sink does not implement OUTL_IActorInputSink: " + behaviour.GetType().Name, behaviour); warnings++; continue; }
                OUTL_IActorInputPhasedSink phased = sink as OUTL_IActorInputPhasedSink;
                OUTL_ActorInputPhase phase = phased != null ? phased.Phase : OUTL_ActorInputPhase.Interaction;
                if ((int)phase < lastPhase)
                {
                    LogWarning("ActorControlBridge input sinks are not phase-sorted. Movement/Aim must run before Weapon, Weapon before Interaction.", bridge);
                    warnings++;
                    break;
                }
                lastPhase = (int)phase;
                if (phase == OUTL_ActorInputPhase.Movement || phase == OUTL_ActorInputPhase.Aim) hasMovement = true;
                if (phase == OUTL_ActorInputPhase.Weapon) hasWeapon = true;
            }

            if (!hasMovement) { LogWarning("ActorControlBridge has no Movement/Aim phase sink. Actor may not move/aim through the input contract.", bridge); warnings++; }
            if (bridge.GetComponent<OUTL_AttackDriver>() != null && !hasWeapon) { LogWarning("armed ActorControlBridge has no Weapon phase sink. Add OUTL_AttackDriverInputSink.", bridge); warnings++; }
        }
        return warnings;
    }

    private static int ValidateAttackDriverStack(OUTL_AttackDriver attack)
    {
        int warnings = 0;
        if (attack == null) return 0;
        if (attack.Muzzle == null) { LogWarning("OUTL_AttackDriver with no Muzzle.", attack); warnings++; }
        if (attack.Primary == null && attack.Secondary == null && attack.Melee == null) { LogWarning("OUTL_AttackDriver with no attack profiles.", attack); warnings++; }
        OUTL_AIActor ai = attack.GetComponent<OUTL_AIActor>();
        if (ai != null && ai.PreferRangedCombat && attack.Primary == null) { LogWarning("ranged actor missing Primary OUTL_AttackProfile.", attack); warnings++; }
        warnings += ValidateAttackProfile("Primary", attack.Primary, attack);
        warnings += ValidateAttackProfile("Secondary", attack.Secondary, attack);
        warnings += ValidateAttackProfile("Melee", attack.Melee, attack);
        return warnings;
    }

    private static int ValidateAttackProfile(string slot, OUTL_AttackProfile profile, Object context)
    {
        if (profile == null) return 0;
        int warnings = 0;
        if (profile.Mode == OUTL_AttackMode.Projectile)
        {
            if (profile.ProjectilePrefab == null)
            {
                LogWarning("OUTL_AttackDriver " + slot + " projectile profile has no ProjectilePrefab.", context);
                warnings++;
            }
            else if (profile.ProjectilePrefab.GetComponent<OUTL_Projectile>() == null)
            {
                LogWarning("OUTL_AttackDriver " + slot + " projectile prefab is missing OUTL_Projectile. Runtime component fallback is not allowed.", profile.ProjectilePrefab);
                warnings++;
            }
            else
            {
                Collider[] colliders = profile.ProjectilePrefab.GetComponentsInChildren<Collider>(true);
                if (colliders == null || colliders.Length == 0)
                {
                    LogWarning("OUTL_AttackDriver " + slot + " projectile prefab has OUTL_Projectile but no Collider. It can move, but trigger/contact gameplay will not resolve.", profile.ProjectilePrefab);
                    warnings++;
                }
                if (!HasPoolReset(profile.ProjectilePrefab))
                {
                    LogWarning("OUTL_AttackDriver " + slot + " projectile prefab has no OUTL_IPoolReset component. OUTL_Projectile normally provides this; check prefab authoring.", profile.ProjectilePrefab);
                    warnings++;
                }
            }
        }
        return warnings;
    }

    private static int ValidateAICombatStack(OUTL_AIActor ai)
    {
        int warnings = 0;
        if (ai == null) return 0;
        GameObject go = ai.gameObject;
        OUTL_EntityAdapter entity = go.GetComponent<OUTL_EntityAdapter>();
        OUTL_AttackDriver attack = go.GetComponent<OUTL_AttackDriver>();
        OUTL_ActorControlBridge bridge = go.GetComponent<OUTL_ActorControlBridge>();
        OUTL_BotInputDriver botInput = go.GetComponent<OUTL_BotInputDriver>();
        if (entity == null) { LogWarning("AI '" + go.name + "' missing OUTL_EntityAdapter.", go); warnings++; }
        if (ai.Profile == null) { LogWarning("AI '" + go.name + "' missing OUTL_AIProfile.", ai); warnings++; }
        else if (!HasAITargetAcquisitionPath(ai, entity)) { LogWarning("AI '" + go.name + "' has no clear target acquisition path. Use faction hostility with Faction or profile EnemyTags.", ai); warnings++; }
        if (HasAISchedules(ai) && !ai.UseStimulusInterrupts) { LogWarning("Actor has schedule/goals but no stimulus interruption path. Enable UseStimulusInterrupts.", ai); warnings++; }
        if (go.GetComponent<OUTL_HearingSensor>() == null && go.GetComponent<OUTL_StimulusSensor>() == null) { LogWarning("AI actor has no stimulus sensor path. Add OUTL_StimulusSensor or OUTL_HearingSensor so perception is scheduler/bus driven.", ai); warnings++; }
        if (go.GetComponent<OUTL_DamageReceiver>() == null) { LogWarning("AI '" + go.name + "' missing OUTL_DamageReceiver.", go); warnings++; }
        if (go.GetComponent<OUTL_Vitals>() == null) { LogWarning("AI '" + go.name + "' missing OUTL_Vitals.", go); warnings++; }
        if (go.GetComponent<OUTL_DeathHandler>() == null && go.GetComponent<OUTL_DeathRuntime>() == null) { LogWarning("AI '" + go.name + "' missing death component. Killed NPCs may keep thinking/attacking.", go); warnings++; }
        OUTL_NavMeshMover mover = go.GetComponent<OUTL_NavMeshMover>();
        if (!ai.Stationary && mover == null && go.GetComponent<OUTL_ActorControlBridge>() == null) { LogWarning("AI '" + go.name + "' missing OUTL_NavMeshMover or ActorControlBridge movement path.", go); warnings++; }
        if (mover != null && !mover.UseOUTLTick && mover.AllowLegacyUpdateTick) { LogWarning("AI '" + go.name + "' NavMeshMover uses legacy Update tick. Keep UseOUTLTick enabled for canonical scheduler-driven movement.", mover); warnings++; }
        if (ai.UseActorInputContract)
        {
            if (botInput == null) { LogWarning("AI '" + go.name + "' uses actor input contract but has no OUTL_BotInputDriver.", ai); warnings++; }
            if (bridge == null)
            {
                LogWarning("AI '" + go.name + "' uses actor input contract but has no OUTL_ActorControlBridge.", ai);
                warnings++;
            }
            else
            {
                if (botInput != null && bridge.InputSourceBehaviour != botInput) { LogWarning("AI '" + go.name + "' actor bridge source is not its OUTL_BotInputDriver. Possession/override is allowed only when intentional.", bridge); warnings++; }
                if (!ai.Stationary && !BridgeHasSink<OUTL_NavMoverInputSink>(bridge)) { LogWarning("AI '" + go.name + "' uses actor input contract but bridge has no OUTL_NavMoverInputSink movement sink.", bridge); warnings++; }
                if (attack != null && !BridgeHasSink<OUTL_AimInputSink>(bridge)) { LogWarning("armed AI '" + go.name + "' uses actor input contract but bridge has no OUTL_AimInputSink.", bridge); warnings++; }
                if (attack != null && !BridgeHasSink<OUTL_AttackDriverInputSink>(bridge)) { LogWarning("armed AI '" + go.name + "' uses actor input contract but bridge has no OUTL_AttackDriverInputSink.", bridge); warnings++; }
                if (!bridge.UseUnityUpdateForLocalInput || !bridge.ApplyNearActorsEveryFrame || bridge.LocalPlayerUpdateMode == OUTL_ActorInputUpdateMode.SchedulerOnly)
                {
                    LogWarning("AI '" + go.name + "' actor bridge is not configured for every-frame Full/Near input. Close combat NPCs may feel delayed.", bridge);
                    warnings++;
                }
            }

            OUTL_NavMoverInputSink navSink = go.GetComponent<OUTL_NavMoverInputSink>();
            if (!ai.Stationary && navSink != null)
            {
                if (!navSink.StopOnlyOwnedDestination) { LogWarning("AI '" + go.name + "' NavMoverInputSink may stop destinations owned by schedule/behavior. Enable StopOnlyOwnedDestination.", navSink); warnings++; }
                if (navSink.MovementAuthority != "actor_input") { LogWarning("AI '" + go.name + "' NavMoverInputSink MovementAuthority should be actor_input for debug/authority clarity.", navSink); warnings++; }
                if (!navSink.OverrideOwnedStopDistance) { LogWarning("AI '" + go.name + "' NavMoverInputSink should override owned stop distance for smooth actor-input micro destinations.", navSink); warnings++; }
                else if (navSink.OwnedStopDistance > 0.35f) { LogWarning("AI '" + go.name + "' NavMoverInputSink OwnedStopDistance is high; close NPCs may brake/jitter during actor-input movement.", navSink); warnings++; }
            }
        }
        else if (botInput != null || go.GetComponent<OUTL_TacticalPlanner>() != null)
        {
            LogWarning("AI '" + go.name + "' has tactical/input components but UseActorInputContract is disabled. Prefer one modular input path for NPC control.", ai);
            warnings++;
        }
        if (attack == null && ai.PreferRangedCombat) { LogWarning("Ranged AI actor '" + go.name + "' missing OUTL_AttackDriver.", go); warnings++; }
        if (attack != null)
        {
            if (attack.Source == null) { LogWarning("AI '" + go.name + "' AttackDriver.Source is null.", attack); warnings++; }
            if (attack.Muzzle == null) { LogWarning("AI '" + go.name + "' AttackDriver missing Muzzle transform.", attack); warnings++; }
            if (attack.Primary == null && attack.Secondary == null && attack.Melee == null) { LogWarning("AI '" + go.name + "' AttackDriver has no attack profile.", attack); warnings++; }
            if (ai.PreferRangedCombat && attack.Primary == null) { LogWarning("Ranged AI actor '" + go.name + "' missing Primary AttackProfile.", attack); warnings++; }
        }
        if (entity != null)
        {
            if (entity.Faction == null && (entity.Def == null || entity.Def.Tags == null || entity.Def.Tags.Length == 0)) { LogWarning("AI '" + go.name + "' has no faction and no Def tags for target selection.", entity); warnings++; }
            if (!entity.RegisterInSectors) { LogWarning("AI '" + go.name + "' is not registered in sectors; perception will not scale properly.", entity); warnings++; }
        }
        return warnings;
    }

    private static int ValidateNPCBehaviorStack(OUTL_NPCBehaviorController npc)
    {
        int warnings = 0;
        if (npc == null) return 0;
        if (npc.Entity == null && npc.GetComponent<OUTL_EntityAdapter>() == null) { LogWarning("NPCBehaviorController has no Entity/EntityAdapter.", npc); warnings++; }
        if (npc.AIActor == null && npc.GetComponent<OUTL_AIActor>() == null) { LogWarning("NPCBehaviorController has no AIActor reference/component.", npc); warnings++; }
        if (npc.Model == null) { LogWarning("NPCBehaviorController has no behavior model. Abstract travel/schedules will be weak.", npc); warnings++; }
        if (npc.UseSharedRouteCache == false && npc.UseLocalRouteCache == false) { LogWarning("NPCBehaviorController has no shared or local route cache enabled.", npc); warnings++; }
        OUTL_AIActor ai = npc.AIActor != null ? npc.AIActor : npc.GetComponent<OUTL_AIActor>();
        if (ai != null && ai.UseActorInputContract)
        {
            OUTL_BotInputDriver botInput = npc.GetComponent<OUTL_BotInputDriver>();
            OUTL_ActorControlBridge bridge = npc.GetComponent<OUTL_ActorControlBridge>();
            OUTL_NavMoverInputSink navSink = npc.GetComponent<OUTL_NavMoverInputSink>();
            if (!npc.PreferActorInputForExactMovement) { LogWarning("input-driven NPCBehaviorController has PreferActorInputForExactMovement disabled. Schedule travel may bypass the modular actor input path and jitter.", npc); warnings++; }
            if (botInput == null) { LogWarning("input-driven NPCBehaviorController has no OUTL_BotInputDriver to turn schedule goals into actor input.", npc); warnings++; }
            else if (!botInput.UseNPCScheduleMoveIntent) { LogWarning("input-driven NPCBehaviorController has BotInputDriver.UseNPCScheduleMoveIntent disabled; schedules will not feed movement input.", botInput); warnings++; }
            if (bridge == null) { LogWarning("input-driven NPCBehaviorController has no OUTL_ActorControlBridge for per-frame input application.", npc); warnings++; }
            if (navSink == null && !ai.Stationary) { LogWarning("input-driven NPCBehaviorController has no OUTL_NavMoverInputSink for modular movement handoff.", npc); warnings++; }
            else if (navSink != null)
            {
                if (!navSink.StopOnlyOwnedDestination) { LogWarning("input-driven NPCBehaviorController needs NavMoverInputSink.StopOnlyOwnedDestination so schedule routes are not cancelled by zero input.", navSink); warnings++; }
                if (!navSink.OverrideOwnedStopDistance || navSink.OwnedStopDistance > 0.35f) { LogWarning("input-driven NPCBehaviorController needs NavMoverInputSink tight owned stop distance for smooth per-frame controller movement.", navSink); warnings++; }
            }
        }
        return warnings;
    }

    private static bool BridgeHasSink<T>(OUTL_ActorControlBridge bridge) where T : Behaviour
    {
        if (bridge == null || bridge.InputSinkBehaviours == null) return false;
        for (int i = 0; i < bridge.InputSinkBehaviours.Length; i++)
            if (bridge.InputSinkBehaviours[i] is T)
                return true;
        return false;
    }

    private static bool HasActorTag(OUTL_EntityAdapter entity)
    {
        if (entity == null) return false;
        if (entity.Def != null && entity.Def.Tags != null)
        {
            for (int i = 0; i < entity.Def.Tags.Length; i++)
            {
                string tag = entity.Def.Tags[i];
                if (tag == "Actor" || tag == "NPC" || tag == "Enemy" || tag == "Player" || tag == "Damageable" || tag == "Creature")
                    return true;
            }
        }
        string className = !string.IsNullOrEmpty(entity.ClassNameOverride) ? entity.ClassNameOverride : (entity.Def != null ? entity.Def.ClassName : string.Empty);
        return !string.IsNullOrEmpty(className) && (className.IndexOf("actor", System.StringComparison.OrdinalIgnoreCase) >= 0 || className.IndexOf("npc", System.StringComparison.OrdinalIgnoreCase) >= 0 || className.IndexOf("creature", System.StringComparison.OrdinalIgnoreCase) >= 0 || className.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool HasHealthDefaultPath(OUTL_EntityAdapter entity)
    {
        if (entity == null) return false;
        bool hasHealth = false;
        bool hasMaxHealth = false;
        if (entity.Def != null && entity.Def.BaseStats != null)
        {
            for (int i = 0; i < entity.Def.BaseStats.Length; i++)
            {
                string key = entity.Def.BaseStats[i].Key;
                if (key == "Health" && entity.Def.BaseStats[i].Value > 0f) hasHealth = true;
                if (key == "MaxHealth" && entity.Def.BaseStats[i].Value > 0f) hasMaxHealth = true;
            }
        }

        OUTL_Vitals vitals = entity.GetComponent<OUTL_Vitals>();
        if (vitals != null && vitals.InitializeMissingStats && vitals.DefaultHealth > 0f && vitals.DefaultMaxHealth > 0f)
        {
            hasHealth = true;
            hasMaxHealth = true;
        }

        return hasHealth && hasMaxHealth;
    }

    private static bool HasAISchedules(OUTL_AIActor ai)
    {
        if (ai == null || ai.Profile == null) return false;
        return ai.Profile.IdleSchedule != null || ai.Profile.CombatSchedule != null || ai.Profile.SearchSchedule != null || ai.Profile.FleeSchedule != null;
    }

    private static bool HasAITargetAcquisitionPath(OUTL_AIActor ai, OUTL_EntityAdapter entity)
    {
        if (ai == null || ai.Profile == null) return false;
        if (ai.Profile.EnemyTags != null && ai.Profile.EnemyTags.Length > 0) return true;
        return ai.Profile.UseFactionHostility && entity != null && entity.Faction != null;
    }

    private static int ValidateParallelReadiness(OUTL_ChunkProcessingDriver[] drivers, OUTL_AIActor[] aiActors)
    {
        int warnings = 0;
        if (drivers == null || drivers.Length == 0 || aiActors == null || aiActors.Length == 0) return 0;

        OUTL_ChunkProcessingDriver driver = null;
        for (int i = 0; i < drivers.Length; i++)
        {
            if (drivers[i] == null) continue;
            if (!drivers[i].BuildParallelReadinessSnapshot)
            {
                LogWarning("ChunkProcessingDriver has parallel readiness snapshot disabled; debug row counts will stay zero.", drivers[i]);
                warnings++;
            }
            if (driver == null && drivers[i].Focus != null) driver = drivers[i];
        }

        if (driver == null || driver.Focus == null || driver.Profile == null) return warnings;

        OUTL_ProcessingProfile profile = driver.Profile;
        float fullDistance = Mathf.Max(profile.FullDistance, driver.ChunkSize * Mathf.Max(1, driver.FullRadius + 1));
        float warnDistance = fullDistance * 1.5f;
        float warnSqr = warnDistance * warnDistance;

        for (int i = 0; i < aiActors.Length; i++)
        {
            OUTL_AIActor ai = aiActors[i];
            if (ai == null || ai.Profile == null) continue;
            OUTL_EntityAdapter entity = ai.Entity != null ? ai.Entity : ai.GetComponent<OUTL_EntityAdapter>();
            if (entity == null) continue;
            if (entity.Tier != OUTL_RuntimeTier.Full) continue;
            if (ai.Profile.ThinkIntervalNear > 0.08f) continue;
            float sqr = (ai.transform.position - driver.Focus.position).sqrMagnitude;
            if (sqr <= warnSqr) continue;
            LogWarning("AIActor has expensive Full-tier near thinking while far from processing focus. Check chunk driver focus/tier preview or lower near tick cost.", ai);
            warnings++;
        }

        return warnings;
    }

    private static int ValidateSectorIntegritySummary(OUTL_World[] worlds)
    {
        if (!Application.isPlaying || worlds == null || worlds.Length != 1 || worlds[0] == null) return 0;
        OUTL_SectorIntegrityStats stats;
        int issues = worlds[0].Sectors.ValidateIntegrity(null, out stats);
        if (issues <= 0) return 0;
        LogWarning("sector integrity summary issues=" + issues + " registryNotSector=" + stats.MissingFromSector + " sectorNotRegistry=" + stats.MissingFromRegistry + " duplicateIds=" + stats.DuplicateSectorEntries + " staleAddress=" + stats.StaleSectorAddress + ". Open OUT CORE Lite -> Workbench -> Sector Integrity Window for details.", worlds[0]);
        return 1;
    }

    private static int ValidateAddressing(OUTL_EntityAdapter e, HashSet<string> targetNames, Dictionary<string, OUTL_EntityAdapter> firstTargetNameOwner, HashSet<string> duplicateTargetNames)
    {
        int warnings = 0;
        if (e == null) return 0;

        if (!string.IsNullOrEmpty(e.ClassNameOverride) && e.ClassNameOverride.Trim() != e.ClassNameOverride) { LogWarning("ClassNameOverride has leading/trailing whitespace: '" + e.ClassNameOverride + "'", e); warnings++; }
        if (!string.IsNullOrEmpty(e.TargetName))
        {
            if (e.TargetName.Trim() != e.TargetName) { LogWarning("TargetName has leading/trailing whitespace: '" + e.TargetName + "'", e); warnings++; }
            if (targetNames.Contains(e.TargetName)) duplicateTargetNames.Add(e.TargetName);
            else targetNames.Add(e.TargetName);
            if (!firstTargetNameOwner.ContainsKey(e.TargetName)) firstTargetNameOwner[e.TargetName] = e;
        }
        if (!string.IsNullOrEmpty(e.Target) && e.Target.Trim() != e.Target) { LogWarning("Target has leading/trailing whitespace: '" + e.Target + "'", e); warnings++; }
        if (!string.IsNullOrEmpty(e.KillTarget) && e.KillTarget.Trim() != e.KillTarget) { LogWarning("KillTarget has leading/trailing whitespace: '" + e.KillTarget + "'", e); warnings++; }
        if (!string.IsNullOrEmpty(e.StableId) && e.StableId.Trim() != e.StableId) { LogWarning("StableId has leading/trailing whitespace: '" + e.StableId + "'", e); warnings++; }
        return warnings;
    }

    private static int ValidateEntityComponentConflicts(OUTL_EntityAdapter e)
    {
        int warnings = 0;
        if (e == null) return 0;
        if (e.GetComponents<OUTL_EntityAdapter>().Length > 1) { LogError("multiple OUTL_EntityAdapter components on one GameObject", e); warnings++; }
        if (e.GetComponent<OUTL_Button>() != null && e.GetComponent<OUTL_TestChest>() != null) { LogWarning("Entity has both Button and TestChest. Use command will be handled by both.", e); warnings++; }
        if (e.GetComponent<OUTL_Door>() != null && e.GetComponent<OUTL_TestChest>() != null) { LogWarning("Entity has both Door and TestChest. Open/Close command will be handled by both.", e); warnings++; }
        if (e.GetComponent<OUTL_MultiSource>() != null && e.GetComponent<OUTL_LogicRelay>() != null) { LogWarning("Entity has both MultiSource and LogicRelay. Graph/debug should confirm the intended logic path.", e); warnings++; }
        return warnings;
    }

    private static int ValidateOutgoingEntityAddress(string ownerType, string targetName, HashSet<string> targetNames, Object context)
    {
        if (string.IsNullOrEmpty(targetName)) return 0;
        if (!targetNames.Contains(targetName))
        {
            LogWarning(ownerType + " TargetName not found: " + targetName, context);
            return 1;
        }
        return 0;
    }

    private static int ValidateInteractable(OUTL_Interactable interactable, HashSet<string> targetNames)
    {
        int warnings = 0;
        if (interactable.Entity == null && interactable.GetComponent<OUTL_EntityAdapter>() == null) { LogWarning("Interactable without Entity/EntityAdapter; source identity will be weak.", interactable); warnings++; }
        if (interactable.Outputs != null && interactable.Outputs.Length > 0) warnings += ValidateOutputs("Interactable", interactable.Outputs, targetNames, interactable);
        else if (!interactable.SendToSelf) { LogWarning("Interactable has no Outputs[] and SendToSelf is disabled.", interactable); warnings++; }
        if (interactable.Targets != null && interactable.Targets.Length > 0) { LogWarning("Interactable has legacy direct Targets[] data. Runtime ignores it; migrate to Outputs -> TargetName -> CommandSystem.", interactable); warnings++; }
        int persistentCalls = CountPersistentCalls(interactable.OnUsed) + CountPersistentCalls(interactable.OnPickedUp) + CountPersistentCalls(interactable.OnDropped);
        if (persistentCalls > 0) { LogWarning("Interactable has legacy UnityEvent callbacks. Runtime ignores them; migrate gameplay routing to Outputs.", interactable); warnings++; }
        return warnings;
    }

    private static int ValidateAccessController(OUTL_AccessController access, HashSet<string> targetNames)
    {
        int warnings = 0;
        if (access.Entity == null && access.GetComponent<OUTL_EntityAdapter>() == null)
        {
            LogWarning("AccessController without Entity/EntityAdapter.", access);
            warnings++;
        }
        if (access.StartsLocked && !access.AllowLockedWithoutRequirements && (access.Requirements == null || access.Requirements.Length == 0))
        {
            LogWarning("AccessController starts locked but has no requirements. Only an explicit Unlock command can open it.", access);
            warnings++;
        }
        if (access.GuardedCommands == null || access.GuardedCommands.Length == 0)
        {
            LogWarning("AccessController has no GuardedCommands.", access);
            warnings++;
        }
        if (access.Requirements != null)
        {
            for (int i = 0; i < access.Requirements.Length; i++)
            {
                OUTL_AccessRequirement requirement = access.Requirements[i];
                if (requirement == null || requirement.Condition == null)
                {
                    LogWarning("AccessController has null requirement at index " + i + ".", access);
                    warnings++;
                    continue;
                }
                OUTL_ConditionDef condition = requirement.Condition;
                if (condition.Op == OUTL_ConditionOp.HasItem)
                {
                    if (condition.ItemDef == null) { LogWarning("Access HasItem requirement has no ItemDef at index " + i + ".", access); warnings++; }
                    if (condition.Subject != OUTL_ConditionSubject.Source) { LogWarning("Access HasItem requirement normally must inspect Source (the user), not Target.", access); warnings++; }
                    if (requirement.ConsumeItem && access.ConsumePolicy == OUTL_AccessConsumePolicy.Never) { LogWarning("Access requirement marks ConsumeItem but ConsumePolicy is Never.", access); warnings++; }
                }
            }
        }
        if (access.Outputs != null && access.Outputs.Length > 0)
            warnings += ValidateOutputs("AccessController", access.Outputs, targetNames, access);
        return warnings;
    }

    private static int CountPersistentCalls(UnityEngine.Events.UnityEvent evt)
    {
        return evt != null ? evt.GetPersistentEventCount() : 0;
    }

    private static int ValidateOutputs(string ownerType, OUTL_OutputLink[] outputs, HashSet<string> targetNames, Object context)
    {
        int warnings = 0;
        if (outputs == null || outputs.Length == 0) { LogWarning(ownerType + " without Outputs[]", context); return 1; }
        for (int o = 0; o < outputs.Length; o++)
        {
            OUTL_OutputLink output = outputs[o];
            if (output == null) { LogWarning(ownerType + " output is null at index " + o, context); warnings++; continue; }
            if (output.Disabled) continue;
            if (string.IsNullOrEmpty(output.EventName)) { LogWarning(ownerType + " output without EventName at index " + o, context); warnings++; }
            if (!string.IsNullOrEmpty(output.TargetName) && output.TargetName.Trim() != output.TargetName) { LogWarning(ownerType + " output TargetName has leading/trailing whitespace at index " + o + ": '" + output.TargetName + "'", context); warnings++; }
            if (string.IsNullOrEmpty(output.TargetName)) { LogWarning(ownerType + " output without TargetName at index " + o, context); warnings++; }
            else if (!targetNames.Contains(output.TargetName)) { LogWarning(ownerType + " output TargetName not found: " + output.TargetName, context); warnings++; }
            if (output.Command == OUTL_CommandType.None) { LogWarning(ownerType + " output Command is None at index " + o, context); warnings++; }
        }
        return warnings;
    }

    private static bool HasPoolReset(GameObject prefab)
    {
        if (prefab == null) return false;
        MonoBehaviour[] behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++) if (behaviours[i] is OUTL_IPoolReset) return true;
        return false;
    }

    private static int ValidatePhysicsGroundingInternal(bool verbose)
    {
        int warnings = 0;
        Collider[] colliders = Object.FindObjectsOfType<Collider>(true);
        bool hasSolidGround = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null || c.isTrigger || !c.enabled) continue;
            string n = c.gameObject.name.ToLowerInvariant();
            if (n.IndexOf("ground") >= 0 || n.IndexOf("floor") >= 0 || n.IndexOf("terrain") >= 0)
            {
                hasSolidGround = true;
                break;
            }
        }
        if (!hasSolidGround) { LogWarning("scene has no obvious non-trigger ground/floor/terrain collider. NavMesh is not a physics collider; actors can fall through if no solid Collider exists.", null); warnings++; }

        OUTL_EntityAdapter[] entities = Object.FindObjectsOfType<OUTL_EntityAdapter>(true);
        for (int i = 0; i < entities.Length; i++)
        {
            OUTL_EntityAdapter entity = entities[i];
            if (entity == null || !HasActorTag(entity)) continue;
            CharacterController cc = entity.GetComponent<CharacterController>();
            if (cc != null)
            {
                float bottom = cc.bounds.min.y;
                RaycastHit hit;
                Vector3 origin = entity.transform.position + Vector3.up * 2f;
                if (Physics.Raycast(origin, Vector3.down, out hit, 16f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (bottom < hit.point.y - 0.1f) { LogWarning("CharacterController bottom appears below nearest ground by more than 0.1m. Check root Y / center / height.", cc); warnings++; }
                }
                else if (verbose)
                {
                    LogWarning("CharacterController actor has no ground hit below it. It may fall forever unless this is intentional.", cc);
                    warnings++;
                }
            }

            NavMeshAgent agent = entity.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                NavMeshHit navHit;
                if (!NavMesh.SamplePosition(entity.transform.position, out navHit, 2f, NavMesh.AllAreas))
                {
                    LogWarning("NavMeshAgent actor is not near a baked NavMesh. Bake navmesh or move actor onto a navigable surface.", agent);
                    warnings++;
                }
            }
        }
        return warnings;
    }

    private static int ValidateRuntimeConstructionSource()
    {
        string root = Path.Combine(Application.dataPath, "OUT", "OUT_Core", "OUT_CORE_Lite");
        if (!Directory.Exists(root)) return 0;

        int warnings = 0;
        string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string relativePath = NormalizeSourcePath(root, files[i]);
            if (!IsGameplayRuntimeSource(relativePath)) continue;
            warnings += ValidateRuntimeConstructionFile(files[i], relativePath);
        }
        return warnings;
    }

    private static int ValidateRuntimeConstructionFile(string file, string relativePath)
    {
        int warnings = 0;
        string[] lines;
        try { lines = File.ReadAllLines(file); }
        catch (IOException) { return 0; }

        for (int i = 0; i < lines.Length; i++)
        {
            string code = StripLineComment(lines[i]);
            string token;
            if (!TryFindRuntimeConstructionPattern(code, out token)) continue;
            LogWarning("runtime gameplay source uses '" + token + "' in " + relativePath + ":" + (i + 1) + ". Route object lifetime through OUTL_World, OUTL_PoolSystem or OutCore.pool.OUT.", null);
            warnings++;
        }
        return warnings;
    }

    private static bool TryFindRuntimeConstructionPattern(string code, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrWhiteSpace(code)) return false;
        if (IsCanonicalPoolFacadeUse(code)) return false;
        if (code.IndexOf("UnityEngine.Object.Instantiate", System.StringComparison.Ordinal) >= 0 || ContainsCall(code, "Instantiate")) { token = "Instantiate"; return true; }
        if (code.IndexOf("new GameObject", System.StringComparison.Ordinal) >= 0) { token = "new GameObject"; return true; }
        if (ContainsCall(code, "Destroy")) { token = "Destroy"; return true; }
        if (code.IndexOf("AddComponent<", System.StringComparison.Ordinal) >= 0 || code.IndexOf(".AddComponent(", System.StringComparison.Ordinal) >= 0) { token = "AddComponent"; return true; }
        if (code.IndexOf("Resources.Load", System.StringComparison.Ordinal) >= 0) { token = "Resources.Load"; return true; }
        if (code.IndexOf("Resources.GetBuiltinResource", System.StringComparison.Ordinal) >= 0) { token = "Resources.GetBuiltinResource"; return true; }
        if (code.IndexOf("GameObject.Find(", System.StringComparison.Ordinal) >= 0) { token = "GameObject.Find"; return true; }
        if (ContainsCall(code, "FindObjectOfType")) { token = "FindObjectOfType"; return true; }
        if (ContainsCall(code, "FindObjectsOfType")) { token = "FindObjectsOfType"; return true; }
        return false;
    }

    private static bool IsCanonicalPoolFacadeUse(string code)
    {
        if (string.IsNullOrEmpty(code)) return false;
        return code.IndexOf("OutCore.pool.OUT.", System.StringComparison.Ordinal) >= 0
            || code.IndexOf("OUT.Instantiate", System.StringComparison.Ordinal) >= 0
            || code.IndexOf("OUT.Destroy", System.StringComparison.Ordinal) >= 0
            || code.IndexOf("OUT.Release", System.StringComparison.Ordinal) >= 0
            || code.IndexOf("OUTL_PoolSystem.SpawnShared", System.StringComparison.Ordinal) >= 0
            || code.IndexOf("OUTL_PoolSystem.ReleaseShared", System.StringComparison.Ordinal) >= 0;
    }

    private static bool ContainsCall(string code, string methodName)
    {
        string needle = methodName + "(";
        int index = -1;
        while (true)
        {
            index = code.IndexOf(needle, index + 1, System.StringComparison.Ordinal);
            if (index < 0) return false;
            if (index == 0) return true;
            char previous = code[index - 1];
            if (!char.IsLetterOrDigit(previous) && previous != '_') return true;
        }
    }

    private static string StripLineComment(string line)
    {
        if (string.IsNullOrEmpty(line)) return string.Empty;
        int comment = line.IndexOf("//", System.StringComparison.Ordinal);
        return comment >= 0 ? line.Substring(0, comment) : line;
    }

    private static string NormalizeSourcePath(string root, string file)
    {
        string r = root.Replace('\\', '/').TrimEnd('/');
        string f = file.Replace('\\', '/');
        if (f.StartsWith(r, System.StringComparison.OrdinalIgnoreCase)) return f.Substring(r.Length).TrimStart('/');
        return f;
    }

    private static bool IsGameplayRuntimeSource(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return false;
        string p = relativePath.Replace('\\', '/');
        if (StartsWithPath(p, "Editor/")) return false;
        if (StartsWithPath(p, "Debug/")) return false;
        if (StartsWithPath(p, "StressTest/")) return false;
        if (StartsWithPath(p, "Console/")) return false;
        if (StartsWithPath(p, "Templates/")) return false;
        if (StartsWithPath(p, "Worldgen/")) return false;
        if (StartsWithPath(p, "Localization/")) return false;
        if (p.EndsWith(".EditorAuthoring.cs", System.StringComparison.OrdinalIgnoreCase)) return false;
        if (p.EndsWith(".Authoring.cs", System.StringComparison.OrdinalIgnoreCase)) return false;
        if (p == "Core/OUTL_PoolSystem.cs") return false;
        if (p == "Core/OUTL_PoolFacade.cs") return false;
        if (p == "Core/OUTL_TickProfile.cs") return false;
        if (p == "Core/OUTL_World.cs") return false;
        return true;
    }

    private static bool StartsWithPath(string path, string prefix)
    {
        return path.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
    }

    private static void LogWarning(string message, Object context)
    {
        Debug.LogWarning("[OUTL Validator] " + message, context);
    }

    private static void LogError(string message, Object context)
    {
        Debug.LogError("[OUTL Validator] " + message, context);
    }
}
#endif
