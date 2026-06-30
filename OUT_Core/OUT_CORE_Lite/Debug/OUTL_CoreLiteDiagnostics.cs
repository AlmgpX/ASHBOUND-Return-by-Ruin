using System.Collections.Generic;
using System.Text;
using UnityEngine;

#pragma warning disable 0618

[DisallowMultipleComponent]
public class OUTL_CoreLiteDiagnostics : MonoBehaviour
{
    public bool RunOnStart;
    public bool DrawOnGUI = true;
    public bool LogReportToConsole = true;
    public bool IncludeInactive = true;
    public bool CheckStableIds = true;
    public bool CheckPotentialDoubleNavTick = true;
    public bool CheckMissingDefinitions = true;
    public bool CheckProcessingSetup = true;
    public bool CheckDebugViews = true;
    public int MaxPrintedIssues = 32;

    private readonly List<string> issues = new List<string>(64);
    private readonly Dictionary<string, OUTL_StableEntityId> stableIds = new Dictionary<string, OUTL_StableEntityId>(256);
    private string lastReport = "OUTL diagnostics not run";
    private float lastRunUnscaledTime;

    private void Start()
    {
        if (RunOnStart) RunDiagnostics();
    }

    private void OnGUI()
    {
        if (!DrawOnGUI) return;
        GUI.Box(new Rect(12, 132, 760, 180), "OUT CORE Lite Diagnostics");
        GUI.Label(new Rect(22, 154, 740, 150), lastReport);
    }

    [ContextMenu("Run OUT CORE Lite Diagnostics")]
    public void RunDiagnostics()
    {
        issues.Clear();
        stableIds.Clear();
        lastRunUnscaledTime = Time.unscaledTime;

        OUTL_World[] worlds = FindObjectsOfType<OUTL_World>(IncludeInactive);
        OUTL_EntityAdapter[] entities = FindObjectsOfType<OUTL_EntityAdapter>(IncludeInactive);
        OUTL_AIActor[] aiActors = FindObjectsOfType<OUTL_AIActor>(IncludeInactive);
        OUTL_NavMeshMover[] movers = FindObjectsOfType<OUTL_NavMeshMover>(IncludeInactive);
        OUTL_EntityDiary[] diaries = FindObjectsOfType<OUTL_EntityDiary>(IncludeInactive);
        OUTL_ChunkProcessingDriver[] chunkDrivers = FindObjectsOfType<OUTL_ChunkProcessingDriver>(IncludeInactive);
        OUTL_ProcessingDistanceDriver[] legacyProcessingDrivers = FindObjectsOfType<OUTL_ProcessingDistanceDriver>(IncludeInactive);
        OUTL_SectorGridDebugView[] sectorViews = FindObjectsOfType<OUTL_SectorGridDebugView>(IncludeInactive);
        OUTL_StableEntityId[] stableIdComponents = FindObjectsOfType<OUTL_StableEntityId>(IncludeInactive);
        OUTL_Dropper[] droppers = FindObjectsOfType<OUTL_Dropper>(IncludeInactive);
        OUTL_AttackDriver[] attackDrivers = FindObjectsOfType<OUTL_AttackDriver>(IncludeInactive);

        if (worlds.Length == 0) AddIssue("WORLD", "No OUTL_World found in scene.");
        if (worlds.Length > 1) AddIssue("WORLD", "Multiple OUTL_World instances found: " + worlds.Length);

        OUTL_World world = OUTL_World.Instance != null ? OUTL_World.Instance : (worlds.Length > 0 ? worlds[0] : null);

        int dormant = 0, far = 0, mid = 0, near = 0, full = 0;
        int registeredRuntime = 0;
        int randomEnabled = 0;
        int sectorEnabled = 0;
        int missingDef = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            OUTL_EntityAdapter e = entities[i];
            if (e == null) continue;
            if (e.Runtime != null) registeredRuntime++;
            if (e.RegisterRandomTick) randomEnabled++;
            if (e.RegisterInSectors) sectorEnabled++;
            if (CheckMissingDefinitions && e.Def == null)
            {
                missingDef++;
                AddIssue("ENTITY", Path(e) + " has OUTL_EntityAdapter but Def is null.");
            }

            OUTL_RuntimeTier tier = e.Runtime != null ? e.Runtime.Tier : e.Tier;
            if (tier == OUTL_RuntimeTier.Dormant) dormant++;
            else if (tier == OUTL_RuntimeTier.Far) far++;
            else if (tier == OUTL_RuntimeTier.Mid) mid++;
            else if (tier == OUTL_RuntimeTier.Near) near++;
            else full++;
        }

        if (CheckStableIds)
            CheckStableIdDuplicates(stableIdComponents);

        for (int i = 0; i < aiActors.Length; i++)
            CheckAIActor(aiActors[i]);

        for (int i = 0; i < movers.Length; i++)
            CheckMover(movers[i]);

        for (int i = 0; i < diaries.Length; i++)
            CheckDiary(diaries[i]);

        for (int i = 0; i < droppers.Length; i++)
            CheckDropper(droppers[i]);

        for (int i = 0; i < attackDrivers.Length; i++)
            CheckAttackDriver(attackDrivers[i]);

        if (CheckProcessingSetup)
            CheckProcessing(chunkDrivers, legacyProcessingDrivers, world);

        if (CheckDebugViews)
            CheckSectorDebugViews(sectorViews, chunkDrivers);

        StringBuilder sb = new StringBuilder(2048);
        sb.Append("lastRun=").Append(lastRunUnscaledTime.ToString("0.00"));
        sb.Append(" worlds=").Append(worlds.Length);
        sb.Append(" entities=").Append(entities.Length);
        sb.Append(" runtime=").Append(registeredRuntime);
        sb.Append(" ai=").Append(aiActors.Length);
        sb.Append(" movers=").Append(movers.Length);
        sb.Append(" diaries=").Append(diaries.Length);
        sb.Append(" random=").Append(randomEnabled);
        sb.Append(" sectors=").Append(sectorEnabled);
        sb.Append(" chunkProcessingDrivers=").Append(chunkDrivers.Length);
        if (legacyProcessingDrivers.Length > 0) sb.Append(" legacyDistanceDrivers=").Append(legacyProcessingDrivers.Length);
        sb.Append(" sectorDebugViews=").Append(sectorViews.Length);
        sb.Append(" issues=").Append(issues.Count).Append('\n');

        sb.Append("tiers D/F/M/N/Full=")
            .Append(dormant).Append('/')
            .Append(far).Append('/')
            .Append(mid).Append('/')
            .Append(near).Append('/')
            .Append(full);

        if (world != null)
        {
            sb.Append(" | scheduler tickables=").Append(world.Scheduler.TickableCount);
            sb.Append(" randomTickables=").Append(world.Scheduler.RandomTickableCount);
            sb.Append(" worldPaused=").Append(world.IsPaused ? 1 : 0);
        }

        if (missingDef > 0) sb.Append(" | missingDef=").Append(missingDef);

        int max = Mathf.Min(MaxPrintedIssues, issues.Count);
        for (int i = 0; i < max; i++)
            sb.Append('\n').Append(i + 1).Append(") ").Append(issues[i]);

        if (issues.Count > max)
            sb.Append('\n').Append("... ").Append(issues.Count - max).Append(" more issues hidden");

        lastReport = sb.ToString();

        if (LogReportToConsole)
            OUTL_DebugLog.Log(OUTL_DebugChannel.Perf, lastReport, true);
    }

    private void CheckAIActor(OUTL_AIActor ai)
    {
        if (ai == null) return;
        if (ai.Entity == null) AddIssue("AI", Path(ai) + " has no Entity reference.");
        if (ai.Profile == null) AddIssue("AI", Path(ai) + " has no AI Profile.");
        if (ai.UseNavMeshMover && ai.NavMover == null) AddIssue("AI", Path(ai) + " UseNavMeshMover=true but NavMover is null.");
        if (ai.UseAttackDriver && ai.AttackDriver == null) AddIssue("AI", Path(ai) + " UseAttackDriver=true but AttackDriver is null.");

        if (CheckPotentialDoubleNavTick && ai.UseNavMeshMover && ai.NavMover != null && ai.NavMover.UseOUTLTick)
            AddIssue("AI/NAV", Path(ai) + " may double-drive NavMover: AIActor.Update calls TickMove while NavMover.UseOUTLTick=true.");
    }

    private void CheckMover(OUTL_NavMeshMover mover)
    {
        if (mover == null) return;
        if (mover.UseOUTLTick && OUTL_World.Instance == null) AddIssue("NAV", Path(mover) + " uses OUTL tick but no OUTL_World exists.");
        if (mover.TickInterval <= 0f) AddIssue("NAV", Path(mover) + " TickInterval <= 0.");
        if (mover.RepathInterval <= 0f) AddIssue("NAV", Path(mover) + " RepathInterval <= 0.");
        if (mover.ManualAgentTransformUpdate && mover.Agent == null) AddIssue("NAV", Path(mover) + " ManualAgentTransformUpdate=true but Agent is null.");
    }

    private void CheckDiary(OUTL_EntityDiary diary)
    {
        if (diary == null) return;
        if (diary.Entity == null) AddIssue("DIARY", Path(diary) + " has no Entity reference.");
        if (diary.WriteToFile && string.IsNullOrEmpty(diary.FolderName)) AddIssue("DIARY", Path(diary) + " writes to file but FolderName is empty.");
        if (diary.MaxMemoryLines <= 0) AddIssue("DIARY", Path(diary) + " MaxMemoryLines <= 0.");
    }

    private void CheckDropper(OUTL_Dropper dropper)
    {
        if (dropper == null) return;
        if (dropper.DropOnKilled && dropper.Entity == null) AddIssue("LOOT", Path(dropper) + " DropOnKilled=true but Entity is null.");
        if (dropper.DropOnKilled && dropper.DropTable == null) AddIssue("LOOT", Path(dropper) + " DropOnKilled=true but DropTable is null.");
    }

    private void CheckAttackDriver(OUTL_AttackDriver attack)
    {
        if (attack == null) return;
        if (attack.Source == null) AddIssue("COMBAT", Path(attack) + " has no Source entity.");
        if (attack.Primary == null && attack.Secondary == null && attack.Melee == null) AddIssue("COMBAT", Path(attack) + " has no attack profiles assigned.");
    }

    private void CheckProcessing(OUTL_ChunkProcessingDriver[] chunkDrivers, OUTL_ProcessingDistanceDriver[] legacyDrivers, OUTL_World world)
    {
        if (chunkDrivers.Length == 0) AddIssue("PROCESSING", "No OUTL_ChunkProcessingDriver found. Canonical chunk/ring tier dispatch will not run.");
        if (chunkDrivers.Length > 1) AddIssue("PROCESSING", "Multiple OUTL_ChunkProcessingDriver instances found: " + chunkDrivers.Length);
        if (legacyDrivers != null && legacyDrivers.Length > 0) AddIssue("PROCESSING", "Legacy OUTL_ProcessingDistanceDriver is present. Canon is OUTL_ChunkProcessingDriver; remove legacy drivers from production scenes.");

        for (int i = 0; i < chunkDrivers.Length; i++)
        {
            OUTL_ChunkProcessingDriver d = chunkDrivers[i];
            if (d == null) continue;
            if (d.UseAssetProfile && d.ProfileAsset == null) AddIssue("PROCESSING", Path(d) + " UseAssetProfile=true but ProfileAsset is null.");
            if (d.Focus == null && !d.UseRegistryFocusFallback) AddIssue("PROCESSING", Path(d) + " has no Focus and registry fallback is disabled.");
            if (d.Profile == null) AddIssue("PROCESSING", Path(d) + " Profile is null.");
            if (!d.EnforceCanonicalThreeByThree) AddIssue("PROCESSING", Path(d) + " EnforceCanonicalThreeByThree=false. Canon is 3x3 Near, ring2 Mid, ring3 Far, beyond Dormant.");
        }

        if (world != null && chunkDrivers.Length > 0 && world.Scheduler.TickableCount == 0)
            AddIssue("PROCESSING", "World has processing driver but Scheduler.TickableCount is 0. Driver may not be registered yet.");
    }

    private void CheckSectorDebugViews(OUTL_SectorGridDebugView[] views, OUTL_ChunkProcessingDriver[] drivers)
    {
        for (int i = 0; i < views.Length; i++)
        {
            OUTL_SectorGridDebugView view = views[i];
            if (view == null) continue;
            if (view.ChunkDriver == null && view.ProfileAsset == null && drivers.Length > 0)
                AddIssue("DEBUG", Path(view) + " has no ChunkDriver/ProfileAsset assigned; it will use fallback cell size and no tier rings.");
        }
    }

    private void CheckStableIdDuplicates(OUTL_StableEntityId[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            OUTL_StableEntityId id = ids[i];
            if (id == null) continue;
            string value = id.StableId;
            if (string.IsNullOrEmpty(value))
            {
                AddIssue("STABLE_ID", Path(id) + " has empty stable id.");
                continue;
            }

            OUTL_StableEntityId existing;
            if (stableIds.TryGetValue(value, out existing) && existing != null && existing != id)
                AddIssue("STABLE_ID", "Duplicate stable id: " + value + " between " + Path(existing) + " and " + Path(id));
            else
                stableIds[value] = id;
        }
    }

    private void AddIssue(string category, string message)
    {
        issues.Add("[" + category + "] " + message);
    }

    private static string Path(Component c)
    {
        if (c == null) return "null";
        return Path(c.transform);
    }

    private static string Path(Transform t)
    {
        if (t == null) return "null";
        StringBuilder sb = new StringBuilder(128);
        while (t != null)
        {
            if (sb.Length == 0) sb.Insert(0, t.name);
            else sb.Insert(0, t.name + "/");
            t = t.parent;
        }
        return sb.ToString();
    }
}

#pragma warning restore 0618
