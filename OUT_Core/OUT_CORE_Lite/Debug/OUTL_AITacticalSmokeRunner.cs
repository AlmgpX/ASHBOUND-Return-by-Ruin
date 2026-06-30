using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_AITacticalSmokeRunner : MonoBehaviour
{
    public bool RunOnStart;
    public bool CleanupAfterRun = true;
    public bool LogSuccess = true;

    private int failures;
    private readonly List<GameObject> createdObjects = new List<GameObject>(64);

    private void Start()
    {
        if (RunOnStart) RunAll();
    }

    [ContextMenu("OUT Run Tactical AI Smoke")]
    public void RunAll()
    {
        failures = 0;
        ClearSmokeObjects();
        OUTL_World world = EnsureWorld();
        OUTL_StimulusBus.Clear();

        SmokeDeadBotDoesNotFire(world);
        SmokeActorInputPhaseOrder(world);
        SmokeFireAuthorizationBlocks(world);
        SmokeBadAimAngleBlocks(world);
        SmokeVisibleTargetCreatesFire(world);
        SmokeFriendlyFireBlocks(world);
        SmokeCoverRegistry(world);
        SmokeCoverRegistrySectorBuckets(world);
        SmokeSquadBlackboardCover(world);
        SmokeSquadFireLaneReservation(world);
        SmokeDispatcherBudgetRespected(world);
        SmokeFarAndDormantNoBotInput(world);
        SmokeLeapAbilityInput(world);
        SmokeLeapAbilityCooldownDeathAuthority(world);
        SmokeAimHold(world);
        SmokeDangerChoosesFindCover(world);
        SmokeSuppressOrder(world);

        if (failures == 0 && LogSuccess) Debug.Log("[OUTL Tactical AI Smoke] OK", this);
        else if (failures > 0) Debug.LogError("[OUTL Tactical AI Smoke] failed=" + failures, this);
        if (CleanupAfterRun) ClearSmokeObjects();
    }

    [ContextMenu("OUT Clear Tactical Smoke Objects")]
    public void ClearSmokeObjects()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
            DestroySmokeObject(createdObjects[i]);
        createdObjects.Clear();

        GameObject[] all = GameObject.FindObjectsOfType<GameObject>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && (all[i].name.StartsWith("OUTL_TacticalSmoke_") || all[i].name.StartsWith("OUTL_Smoke_Tactical_")))
                DestroySmokeObject(all[i]);
    }

    private void SmokeDeadBotDoesNotFire(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("DeadBot", world, new Vector3(0f, 0f, 0f), true);
        OUTL_EntityAdapter target = CreateTacticalActor("DeadBotTarget", world, new Vector3(0f, 0f, 8f), false);
        bot.GetComponent<OUTL_AIActor>().CurrentTarget = target.Id;
        bot.GetComponent<OUTL_AIActor>().CurrentTargetVisible = true;
        OUTL_Combat.ApplyDamage(OUTL_EntityId.None, bot.Id, 200f, bot.transform.position, "smoke_dead");
        float hp = target.Runtime.Stats.Get(OUTL_StatId.Health, 0f);
        bot.GetComponent<OUTL_ActorControlBridge>().OUTL_Tick(world, world.WorldTime + 1f, 0.1f);
        if (target.Runtime.Stats.Get(OUTL_StatId.Health, 0f) < hp) Fail("dead bot fired through actor input bridge");
    }

    private void SmokeActorInputPhaseOrder(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("PhaseBot", world, new Vector3(2f, 0f, 0f), true);
        OUTL_EntityAdapter target = CreateTacticalActor("PhaseTarget", world, new Vector3(2f, 0f, 8f), false);
        OUTL_AIActor ai = bot.GetComponent<OUTL_AIActor>();
        ai.CurrentTarget = target.Id;
        ai.CurrentTargetVisible = true;
        ai.LastKnownTargetPosition = target.transform.position;

        OUTL_ActorControlBridge bridge = bot.GetComponent<OUTL_ActorControlBridge>();
        bridge.OUTL_Tick(world, world.WorldTime + 1f, 0.1f);
        int expected = (((0 * 31 + (int)OUTL_ActorInputPhase.Movement) * 31 + (int)OUTL_ActorInputPhase.Aim) * 31 + (int)OUTL_ActorInputPhase.Weapon) * 31 + (int)OUTL_ActorInputPhase.Weapon;
        if (bridge.LastAppliedSinkCount != 4 || bridge.LastAppliedPhaseOrderHash != expected)
            Fail("actor input bridge did not apply Movement/Aim before Weapon. count=" + bridge.LastAppliedSinkCount + " hash=" + bridge.LastAppliedPhaseOrderHash);
    }

    private void SmokeFireAuthorizationBlocks(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("UnauthorizedFireBot", world, new Vector3(3f, 0f, 0f), true);
        OUTL_AttackDriverInputSink sink = bot.GetComponent<OUTL_AttackDriverInputSink>();
        OUTL_ActorInputFrame frame = OUTL_ActorInputFrame.Empty(world.WorldTime + 1f);
        frame.FirePrimaryPressed = true;
        frame.AimWorldPoint = bot.transform.position + bot.transform.forward * 8f;
        frame.HasAimWorldPoint = true;
        frame.FireAuthorized = false;
        sink.OUTL_ApplyInput(frame, world);
        if (sink.LastBlockedReason != "fire_not_authorized") Fail("FireAuthorized=false did not block attack sink");
    }

    private void SmokeBadAimAngleBlocks(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("BadAngleBot", world, new Vector3(4f, 0f, 0f), true);
        OUTL_AttackDriverInputSink sink = bot.GetComponent<OUTL_AttackDriverInputSink>();
        OUTL_ActorInputFrame frame = OUTL_ActorInputFrame.Empty(world.WorldTime + 1f);
        frame.FirePrimaryPressed = true;
        frame.FireAuthorized = true;
        frame.MaxAllowedFireAngle = 1f;
        frame.AimWorldPoint = bot.transform.position - bot.transform.forward * 8f;
        frame.HasAimWorldPoint = true;
        sink.OUTL_ApplyInput(frame, world);
        if (sink.LastBlockedReason != "aim_angle") Fail("bad aim angle did not block FireAt");
    }

    private void SmokeVisibleTargetCreatesFire(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("Shooter", world, new Vector3(4f, 0f, 0f), true);
        OUTL_EntityAdapter target = CreateTacticalActor("Target", world, new Vector3(4f, 0f, 8f), false);
        OUTL_AIActor ai = bot.GetComponent<OUTL_AIActor>();
        ai.CurrentTarget = target.Id;
        ai.CurrentTargetVisible = true;
        ai.LastKnownTargetPosition = target.transform.position;
        bot.GetComponent<OUTL_TacticalPlanner>().OUTL_Tick(world, world.WorldTime + 1f, 0.1f);
        float hp = target.Runtime.Stats.Get(OUTL_StatId.Health, 0f);
        bot.GetComponent<OUTL_ActorControlBridge>().OUTL_Tick(world, world.WorldTime + 1.2f, 0.1f);
        if (target.Runtime.Stats.Get(OUTL_StatId.Health, 0f) >= hp) Fail("visible target did not receive actor-input fire");
    }

    private void SmokeFriendlyFireBlocks(OUTL_World world)
    {
        OUTL_FactionDef friendly = CreateFaction("friendly");
        OUTL_FactionDef hostile = CreateFaction("hostile");
        friendly.Relations = new[] { new OUTL_FactionRelation { Faction = hostile, Relation = -1f } };
        hostile.Relations = new[] { new OUTL_FactionRelation { Faction = friendly, Relation = -1f } };

        OUTL_EntityAdapter bot = CreateTacticalActor("FriendlyFireBot", world, new Vector3(8f, 0f, 0f), true);
        OUTL_EntityAdapter ally = CreateTacticalActor("FriendlyFireAlly", world, new Vector3(8f, 0f, 4f), false);
        OUTL_EntityAdapter target = CreateTacticalActor("FriendlyFireTarget", world, new Vector3(8f, 0f, 8f), false);
        bot.Faction = friendly; bot.RebindRuntime(world);
        ally.Faction = friendly; ally.RebindRuntime(world);
        target.Faction = hostile; target.RebindRuntime(world);
        OUTL_AimProfile aim = bot.GetComponent<OUTL_AimPlanner>().Profile;
        aim.RequireLineOfSight = false;
        aim.UseFriendlyFire = true;

        OUTL_AIActor ai = bot.GetComponent<OUTL_AIActor>();
        ai.CurrentTarget = target.Id;
        ai.CurrentTargetVisible = true;
        ai.LastKnownTargetPosition = target.transform.position;
        bot.GetComponent<OUTL_TacticalPlanner>().OUTL_Tick(world, world.WorldTime + 2f, 0.1f);
        float hp = target.Runtime.Stats.Get(OUTL_StatId.Health, 0f);
        bot.GetComponent<OUTL_ActorControlBridge>().OUTL_Tick(world, world.WorldTime + 2.2f, 0.1f);
        if (target.Runtime.Stats.Get(OUTL_StatId.Health, 0f) < hp) Fail("friendly fire risk did not block fire");
        if (!bot.GetComponent<OUTL_AimPlanner>().CurrentState.FriendlyFireBlocked) Fail("friendly fire state was not visible");
    }

    private void SmokeCoverRegistry(OUTL_World world)
    {
        OUTL_EntityAdapter seeker = CreateTacticalActor("CoverSeeker", world, new Vector3(12f, 0f, 0f), false);
        OUTL_CoverPoint cover = CreateCover("CoverA", new Vector3(12f, 0f, 3f));
        OUTL_CoverQuery query = new OUTL_CoverQuery { Seeker = seeker, SeekerPosition = seeker.transform.position, ThreatPosition = new Vector3(12f, 0f, 8f), SearchRadius = 8f, WeaponRole = OUTL_WeaponRole.Any, VisibilityMask = ~0, RequireBlocksThreat = false, Time = world.WorldTime };
        OUTL_CoverQueryResult[] results = new OUTL_CoverQueryResult[2];
        int count = OUTL_CoverRegistry.QueryNonAlloc(query, results);
        if (count <= 0 || results[0].Point != cover) Fail("cover registry did not return cover point");
        if (!cover.Reserve(seeker, 2f, "smoke")) Fail("cover reservation failed");
    }

    private void SmokeCoverRegistrySectorBuckets(OUTL_World world)
    {
        OUTL_EntityAdapter seeker = CreateTacticalActor("SectorCoverSeeker", world, new Vector3(34f, 0f, 0f), false);
        for (int i = 0; i < 100; i++)
        {
            OUTL_CoverPoint point = CreateCover("SectorCover_" + i, new Vector3(34f + i * 4f, 0f, 6f));
            point.SectorId = i < 10 ? 700 : 900 + i;
        }
        OUTL_CoverRegistry.RebuildAll();
        OUTL_CoverQuery query = new OUTL_CoverQuery { Seeker = seeker, SeekerPosition = seeker.transform.position, SectorId = 700, ThreatPosition = seeker.transform.position + Vector3.forward * 12f, SearchRadius = 32f, WeaponRole = OUTL_WeaponRole.Any, VisibilityMask = ~0, RequireBlocksThreat = false, Time = world.WorldTime };
        OUTL_CoverQueryResult[] results = new OUTL_CoverQueryResult[8];
        int count = OUTL_CoverRegistry.QueryNonAlloc(query, results);
        if (count <= 0) Fail("sector cover query returned no local bucket results");
        if (OUTL_CoverRegistry.LastQueryTouchedPoints >= 100) Fail("sector cover query fell back to scanning all covers");
    }

    private void SmokeSquadBlackboardCover(OUTL_World world)
    {
        GameObject root = CreateSmokeObject("SquadBlackboard", new Vector3(16f, 0f, 0f));
        OUTL_SquadBlackboard blackboard = root.AddComponent<OUTL_SquadBlackboard>();
        OUTL_CoverPoint cover = CreateCover("SquadCover", new Vector3(16f, 0f, 3f));
        OUTL_SquadMember a = CreateTacticalActor("SquadA", world, new Vector3(15f, 0f, 0f), false).gameObject.AddComponent<OUTL_SquadMember>();
        OUTL_SquadMember b = CreateTacticalActor("SquadB", world, new Vector3(17f, 0f, 0f), false).gameObject.AddComponent<OUTL_SquadMember>();
        a.Blackboard = blackboard;
        b.Blackboard = blackboard;
        blackboard.Register(a);
        blackboard.Register(b);
        bool first = a.TryReserveCover(cover, 4f, "smoke");
        bool second = b.TryReserveCover(cover, 4f, "smoke");
        if (!first || second) Fail("squad blackboard allowed duplicate cover reservation");
    }

    private void SmokeSquadFireLaneReservation(OUTL_World world)
    {
        GameObject root = CreateSmokeObject("FireLaneBlackboard", new Vector3(44f, 0f, 0f));
        OUTL_SquadBlackboard blackboard = root.AddComponent<OUTL_SquadBlackboard>();
        OUTL_EntityAdapter a = CreateTacticalActor("FireLaneA", world, new Vector3(44f, 0f, 0f), false);
        OUTL_EntityAdapter b = CreateTacticalActor("FireLaneB", world, new Vector3(44.2f, 0f, 0.2f), false);
        bool first = blackboard.TryReserveFireLane(a, a.transform.position + Vector3.up, new Vector3(44f, 1f, 12f), 1.5f, 2f);
        bool second = blackboard.TryReserveFireLane(b, b.transform.position + Vector3.up, new Vector3(44.1f, 1f, 12f), 1.5f, 2f);
        if (!first || second) Fail("squad blackboard allowed overlapping fire lane reservation");
    }

    private void SmokeDispatcherBudgetRespected(OUTL_World world)
    {
        int oldBudget = world.MaxNpcBehaviorTicksPerFrame;
        int oldRoute = world.MaxNpcRouteUpdatesPerFrame;
        int oldPath = world.MaxNpcPathRequestsPerFrame;
        world.MaxNpcBehaviorTicksPerFrame = 1;
        world.MaxNpcRouteUpdatesPerFrame = 1;
        world.MaxNpcPathRequestsPerFrame = 1;
        for (int i = 0; i < 5; i++)
        {
            OUTL_EntityAdapter npc = CreateTacticalActor("BudgetNPC_" + i, world, new Vector3(50f + i, 0f, 0f), false);
            OUTL_NPCBehaviorController controller = npc.gameObject.AddComponent<OUTL_NPCBehaviorController>();
            controller.Entity = npc;
            controller.Model = ScriptableObject.CreateInstance<OUTL_NPCBehaviorModel>();
            controller.Model.Schedule = ScriptableObject.CreateInstance<OUTL_NPCScheduleDef>();
            controller.Model.NavigationProfile = ScriptableObject.CreateInstance<OUTL_NPCNavigationProfile>();
            controller.Model.Schedule.Entries = new[] { new OUTL_NPCScheduleEntry { EntryId = "budget_travel", Action = OUTL_NPCScheduleActionType.TravelTo, TargetMode = OUTL_NPCScheduleTargetMode.FixedWorldPosition, TargetPosition = npc.transform.position + Vector3.forward * 20f, StartTimeNormalized = 0f, EndTimeNormalized = 1f } };
            npc.Runtime.Tier = OUTL_RuntimeTier.Full;
            controller.Register();
        }
        world.Scheduler.TickLane(OUTL_TickLane.AI, world.WorldTime + 120f, 0.1f);
        OUTL_NPCBehaviorBudgetSnapshot snapshot = OUTL_NPCBehaviorDispatcher.LastSnapshot;
        if (snapshot.TickedThisFrame > 1) Fail("NPC dispatcher exceeded behavior budget. ticked=" + snapshot.TickedThisFrame);
        if (snapshot.SkippedByBudget <= 0) Fail("NPC dispatcher did not report budget skips");
        world.MaxNpcBehaviorTicksPerFrame = oldBudget;
        world.MaxNpcRouteUpdatesPerFrame = oldRoute;
        world.MaxNpcPathRequestsPerFrame = oldPath;
    }

    private void SmokeFarAndDormantNoBotInput(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("DormantBot", world, new Vector3(60f, 0f, 0f), true);
        OUTL_EntityAdapter target = CreateTacticalActor("DormantTarget", world, new Vector3(60f, 0f, 8f), false);
        OUTL_AIActor ai = bot.GetComponent<OUTL_AIActor>();
        ai.CurrentTarget = target.Id;
        ai.CurrentTargetVisible = true;
        OUTL_BotInputDriver driver = bot.GetComponent<OUTL_BotInputDriver>();
        OUTL_ActorInputFrame frame = OUTL_ActorInputFrame.Empty(world.WorldTime);
        bot.Runtime.Tier = OUTL_RuntimeTier.Far;
        if (driver.TryBuildInput(world, bot, world.WorldTime + 1f, 0.1f, ref frame)) Fail("Far NPC produced full bot input");
        bot.Runtime.Tier = OUTL_RuntimeTier.Dormant;
        if (driver.TryBuildInput(world, bot, world.WorldTime + 1f, 0.1f, ref frame)) Fail("Dormant NPC produced bot input");
    }

    private void SmokeLeapAbilityInput(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("LeapBot", world, new Vector3(66f, 0f, 0f), true);
        OUTL_EntityAdapter target = CreateTacticalActor("LeapTarget", world, new Vector3(66f, 0f, 7f), false);
        OUTL_LeapAbilityProfile leap = CreateLeapAbility();
        OUTL_AbilityInputSink ability = bot.GetComponent<OUTL_AbilityInputSink>();
        ability.PrimaryAbility = leap;
        OUTL_TacticalProfile profile = bot.GetComponent<OUTL_TacticalPlanner>().Profile;
        profile.PrimaryAbility = leap;
        profile.LeapAbility = leap;
        OUTL_AIActor ai = bot.GetComponent<OUTL_AIActor>();
        ai.CurrentTarget = target.Id;
        ai.CurrentTargetVisible = true;
        ai.LastKnownTargetPosition = target.transform.position;

        OUTL_TacticalDecision decision = bot.GetComponent<OUTL_TacticalPlanner>().BuildDecision(world, world.WorldTime + 1f, 0.1f);
        if (!decision.WantsAbility || decision.Intent != OUTL_TacticalIntentId.LeapAttack) Fail("leap tactical decision did not request ability input");
        OUTL_ActorInputFrame frame = OUTL_ActorInputFrame.Empty(world.WorldTime + 1f);
        if (!bot.GetComponent<OUTL_BotInputDriver>().TryBuildInput(world, bot, world.WorldTime + 1f, 0.1f, ref frame) || !frame.AbilityPrimaryPressed)
            Fail("bot input did not expose leap ability request");
    }

    private void SmokeLeapAbilityCooldownDeathAuthority(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("LeapGateBot", world, new Vector3(74f, 0f, 0f), true);
        OUTL_LeapAbilityProfile leap = CreateLeapAbility();
        OUTL_AbilityInputSink ability = bot.GetComponent<OUTL_AbilityInputSink>();
        ability.PrimaryAbility = leap;
        OUTL_ActorInputFrame frame = OUTL_ActorInputFrame.Empty(world.WorldTime + 1f);
        frame.AbilityPrimaryPressed = true;
        frame.AbilitySlot = leap.AbilitySlot;
        frame.AbilityTargetPoint = bot.transform.position + Vector3.forward * 6f;
        frame.HasAbilityTargetPoint = true;
        if (!ability.OUTL_CanUseAbility(leap, frame, world)) Fail("fresh leap ability could not start");
        ability.OUTL_ApplyInput(frame, world);
        if (ability.OUTL_CanUseAbility(leap, frame, world)) Fail("leap ability ignored cooldown");

        OUTL_EntityAdapter dead = CreateTacticalActor("LeapDeadBot", world, new Vector3(76f, 0f, 0f), true);
        OUTL_AbilityInputSink deadAbility = dead.GetComponent<OUTL_AbilityInputSink>();
        deadAbility.PrimaryAbility = leap;
        OUTL_Combat.ApplyDamage(OUTL_EntityId.None, dead.Id, 200f, dead.transform.position, "smoke_dead_leap");
        if (deadAbility.OUTL_CanUseAbility(leap, frame, world)) Fail("dead actor can use leap ability");

        OUTL_NetworkSession session = world.GetComponent<OUTL_NetworkSession>();
        if (session == null) session = world.gameObject.AddComponent<OUTL_NetworkSession>();
        session.Mode = OUTL_NetworkMode.Client;
        session.WorldIsClientReplica = true;
        session.WorldIsServerAuthority = false;
        bot.gameObject.AddComponent<OUTL_NetworkIdentityLite>().ServerOwned = true;
        if (ability.OUTL_CanUseAbility(leap, frame, world)) Fail("client replica can use server-owned leap ability");
        session.StartOfflineWorld();
    }

    private void SmokeAimHold(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("AimHoldBot", world, new Vector3(20f, 0f, 0f), true);
        OUTL_EntityAdapter target = CreateTacticalActor("AimHoldTarget", world, new Vector3(20f, 0f, 8f), false);
        OUTL_AimProfile aim = bot.GetComponent<OUTL_AimPlanner>().Profile;
        aim.AimHoldSeconds = 10f;
        OUTL_TacticalDecision decision = new OUTL_TacticalDecision { Intent = OUTL_TacticalIntentId.AttackRanged, Target = target.Id, AimPoint = target.transform.position + Vector3.up, HasAimPoint = true, WantsFire = true, AttackProfile = bot.GetComponent<OUTL_AttackDriver>().Primary, WeaponSlot = OUTL_EquipmentSlot.Primary };
        OUTL_AimState state = bot.GetComponent<OUTL_AimPlanner>().Plan(world, decision, target.Runtime, decision.AttackProfile, world.WorldTime + 3f, 0.1f, null);
        if (state.Command != OUTL_AimCommand.AimOnly) Fail("aim hold did not block immediate fire");
    }

    private void SmokeDangerChoosesFindCover(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("DangerCoverBot", world, new Vector3(24f, 0f, 0f), true);
        OUTL_AIActor ai = bot.GetComponent<OUTL_AIActor>();
        ai.CurrentDanger = 1f;
        ai.LastStimulusType = OUTL_StimulusType.SightDanger;
        ai.LastStimulusPosition = bot.transform.position + Vector3.forward * 8f;
        OUTL_TacticalDecision decision = bot.GetComponent<OUTL_TacticalPlanner>().BuildDecision(world, world.WorldTime + 4f, 0.1f);
        if (decision.Intent != OUTL_TacticalIntentId.FindCover && decision.Intent != OUTL_TacticalIntentId.TakeCover) Fail("danger did not select cover intent");
    }

    private void SmokeSuppressOrder(OUTL_World world)
    {
        OUTL_EntityAdapter bot = CreateTacticalActor("SuppressBot", world, new Vector3(28f, 0f, 0f), true);
        OUTL_EntityAdapter target = CreateTacticalActor("SuppressTarget", world, new Vector3(28f, 0f, 8f), false);
        OUTL_AIActor ai = bot.GetComponent<OUTL_AIActor>();
        ai.CurrentTarget = target.Id;
        ai.CurrentTargetVisible = true;
        ai.CurrentOrder = new OUTL_SquadOrder(OUTL_SquadOrderType.Suppress, target.Id, target.transform.position, 2f, 5f, "smoke_suppress");
        OUTL_TacticalDecision decision = bot.GetComponent<OUTL_TacticalPlanner>().BuildDecision(world, world.WorldTime + 5f, 0.1f);
        if (decision.Intent != OUTL_TacticalIntentId.Suppress || !decision.WantsSuppress) Fail("squad suppress order did not select suppress intent");
    }

    private OUTL_EntityAdapter CreateTacticalActor(string name, OUTL_World world, Vector3 position, bool tactical)
    {
        GameObject go = CreateSmokeObject(name, position);
        go.transform.rotation = Quaternion.identity;
        OUTL_EntityAdapter entity = go.AddComponent<OUTL_EntityAdapter>();
        entity.ClassNameOverride = "actor_generic";
        entity.RegisterNow(world);
        CapsuleCollider collider = go.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.radius = 0.35f;
        OUTL_Hitbox hitbox = go.AddComponent<OUTL_Hitbox>();
        hitbox.Entity = entity;
        OUTL_Vitals vitals = go.AddComponent<OUTL_Vitals>();
        vitals.Entity = entity;
        vitals.DefaultHealth = 100f;
        vitals.DefaultMaxHealth = 100f;
        go.AddComponent<OUTL_DamageReceiver>().Entity = entity;
        go.AddComponent<OUTL_DeathRuntime>().Entity = entity;
        vitals.EnsureInitialized();

        if (tactical)
        {
            OUTL_AttackDriver attack = go.AddComponent<OUTL_AttackDriver>();
            attack.Source = entity;
            attack.Muzzle = go.transform;
            attack.Primary = CreateAttack("smoke.primary", OUTL_AttackMode.Hitscan, 20f, 20f);
            OUTL_AIActor ai = go.AddComponent<OUTL_AIActor>();
            ai.Entity = entity;
            ai.Profile = ScriptableObject.CreateInstance<OUTL_AIProfile>();
            ai.Profile.ViewDistance = 30f;
            ai.Profile.AttackDistance = 12f;
            ai.AttackDriver = attack;
            ai.UseActorInputContract = true;
            OUTL_NavMeshMover nav = go.AddComponent<OUTL_NavMeshMover>();
            nav.UseOUTLTick = true;
            nav.UseTransformFallback = true;
            nav.AffectedByGravity = false;
            ai.NavMover = nav;
            OUTL_TacticalProfile tacticalProfile = ScriptableObject.CreateInstance<OUTL_TacticalProfile>();
            tacticalProfile.AimProfile = CreateAim();
            OUTL_AimPlanner aim = go.AddComponent<OUTL_AimPlanner>();
            aim.Entity = entity;
            aim.AttackDriver = attack;
            aim.Profile = tacticalProfile.AimProfile;
            OUTL_AIArsenalSelector arsenal = go.AddComponent<OUTL_AIArsenalSelector>();
            arsenal.Entity = entity;
            arsenal.AttackDriver = attack;
            OUTL_TacticalPlanner planner = go.AddComponent<OUTL_TacticalPlanner>();
            planner.Entity = entity;
            planner.AIActor = ai;
            planner.Profile = tacticalProfile;
            planner.Arsenal = arsenal;
            planner.AimPlanner = aim;
            OUTL_BotInputDriver bot = go.AddComponent<OUTL_BotInputDriver>();
            bot.Entity = entity;
            bot.AIActor = ai;
            bot.TacticalPlanner = planner;
            bot.AimPlanner = aim;
            bot.Arsenal = arsenal;
            OUTL_AttackDriverInputSink attackSink = go.AddComponent<OUTL_AttackDriverInputSink>();
            attackSink.Entity = entity;
            attackSink.AttackDriver = attack;
            OUTL_NavMoverInputSink navSink = go.AddComponent<OUTL_NavMoverInputSink>();
            navSink.Entity = entity;
            navSink.NavMover = nav;
            OUTL_AimInputSink aimSink = go.AddComponent<OUTL_AimInputSink>();
            aimSink.Entity = entity;
            aimSink.AngularSpeed = tacticalProfile.AimProfile.AimAngularSpeed;
            OUTL_AbilityInputSink abilitySink = go.AddComponent<OUTL_AbilityInputSink>();
            abilitySink.Entity = entity;
            abilitySink.NavMover = nav;
            abilitySink.AllowTransformFallback = true;
            planner.AbilitySink = abilitySink;
            OUTL_ActorControlBridge bridge = go.AddComponent<OUTL_ActorControlBridge>();
            bridge.Entity = entity;
            bridge.InputSourceBehaviour = bot;
            bridge.InputSinkBehaviours = new Behaviour[] { navSink, aimSink, attackSink, abilitySink };
        }

        return entity;
    }

    private OUTL_AttackProfile CreateAttack(string id, OUTL_AttackMode mode, float damage, float range)
    {
        OUTL_AttackProfile profile = ScriptableObject.CreateInstance<OUTL_AttackProfile>();
        profile.AttackId = id;
        profile.Mode = mode;
        profile.Damage = damage;
        profile.Range = range;
        profile.Cooldown = 0f;
        profile.HitDamageKey = id;
        profile.HitMask = ~0;
        return profile;
    }

    private OUTL_AimProfile CreateAim()
    {
        OUTL_AimProfile aim = ScriptableObject.CreateInstance<OUTL_AimProfile>();
        aim.ReactionDelayMin = 0f;
        aim.ReactionDelayMax = 0f;
        aim.FireDelayMin = 0f;
        aim.FireDelayMax = 0f;
        aim.AimHoldSeconds = 0f;
        aim.MaxFireAngleError = 180f;
        aim.AimErrorNear = 180f;
        aim.AimErrorFar = 180f;
        aim.AimSettleTimeMin = 0f;
        aim.AimSettleTimeMax = 0f;
        aim.HoldAimChance = 0f;
        aim.FakeAimChance = 0f;
        aim.AimAngularSpeed = 720f;
        aim.RequireLineOfSight = true;
        aim.UseFriendlyFire = true;
        aim.LineOfFireMask = ~0;
        return aim;
    }

    private OUTL_LeapAbilityProfile CreateLeapAbility()
    {
        OUTL_LeapAbilityProfile leap = ScriptableObject.CreateInstance<OUTL_LeapAbilityProfile>();
        leap.AbilityId = "smoke.leap";
        leap.AbilitySlot = 0;
        leap.MinRange = 2f;
        leap.MaxRange = 12f;
        leap.PreferWhenTargetDistanceMin = 2f;
        leap.PreferWhenTargetDistanceMax = 12f;
        leap.Cooldown = 2f;
        leap.WindupTime = 0f;
        leap.RecoveryTime = 0.2f;
        leap.RequiresLineOfSight = false;
        leap.LeapSpeed = 14f;
        leap.LeapArcHeight = 1.2f;
        leap.LeapDuration = 0.25f;
        leap.ImpactRadius = 1f;
        leap.ImpactDamage = 15f;
        leap.UseCharacterMotor = false;
        leap.UsePhysicsImpulse = false;
        return leap;
    }

    private OUTL_CoverPoint CreateCover(string name, Vector3 position)
    {
        GameObject go = CreateSmokeObject(name, position);
        BoxCollider blocker = go.AddComponent<BoxCollider>();
        blocker.size = new Vector3(2f, 2f, 0.25f);
        OUTL_CoverPoint cover = go.AddComponent<OUTL_CoverPoint>();
        cover.Active = true;
        return cover;
    }

    private OUTL_World EnsureWorld()
    {
        if (OUTL_World.Instance != null) return OUTL_World.Instance;
        GameObject go = CreateSmokeObject("World", Vector3.zero);
        return go.AddComponent<OUTL_World>();
    }

    private OUTL_FactionDef CreateFaction(string id)
    {
        OUTL_FactionDef faction = ScriptableObject.CreateInstance<OUTL_FactionDef>();
        faction.FactionId = id;
        faction.DisplayName = id;
        return faction;
    }

    private GameObject CreateSmokeObject(string name, Vector3 position)
    {
        GameObject go = new GameObject("OUTL_Smoke_Tactical_" + name);
        go.transform.position = position;
        createdObjects.Add(go);
        return go;
    }

    private static void DestroySmokeObject(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    private void Fail(string message)
    {
        failures++;
        Debug.LogError("[OUTL Tactical AI Smoke] " + message, this);
    }
}
