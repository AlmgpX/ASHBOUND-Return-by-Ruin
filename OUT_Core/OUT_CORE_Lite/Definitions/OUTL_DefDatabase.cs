using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "OUT CORE Lite/Def Database", fileName = "OUTL_DefDatabase")]
public class OUTL_DefDatabase : ScriptableObject
{
    public OUTL_EntityDef[] EntityDefs;
    public OUTL_ItemDef[] ItemDefs;
    public OUTL_FactionDef[] Factions;
    public OUTL_QuestDef[] Quests;
    public OUTL_AIProfile[] AIProfiles;
    public OUTL_AttackProfile[] AttackProfiles;
    public OUTL_CharacterTemplate[] CharacterTemplates;

    private readonly Dictionary<string, OUTL_EntityDef> entityById = new Dictionary<string, OUTL_EntityDef>(256);
    private readonly Dictionary<string, OUTL_FactionDef> factionById = new Dictionary<string, OUTL_FactionDef>(64);
    private readonly Dictionary<string, OUTL_QuestDef> questById = new Dictionary<string, OUTL_QuestDef>(64);
    private bool built;

    public void Rebuild()
    {
        entityById.Clear();
        factionById.Clear();
        questById.Clear();

        AddEntities(EntityDefs);
        AddEntities(ItemDefs);

        if (Factions != null)
        {
            for (int i = 0; i < Factions.Length; i++)
            {
                OUTL_FactionDef faction = Factions[i];
                if (faction == null || string.IsNullOrWhiteSpace(faction.FactionId)) continue;
                factionById[faction.FactionId] = faction;
            }
        }

        if (Quests != null)
        {
            for (int i = 0; i < Quests.Length; i++)
            {
                OUTL_QuestDef quest = Quests[i];
                if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId)) continue;
                questById[quest.QuestId] = quest;
            }
        }

        built = true;
    }

    public OUTL_EntityDef FindEntityDef(string id)
    {
        EnsureBuilt();
        if (string.IsNullOrWhiteSpace(id)) return null;
        OUTL_EntityDef def;
        return entityById.TryGetValue(id, out def) ? def : null;
    }

    public OUTL_ItemDef FindItemDef(string id)
    {
        return FindEntityDef(id) as OUTL_ItemDef;
    }

    public OUTL_FactionDef FindFaction(string id)
    {
        EnsureBuilt();
        if (string.IsNullOrWhiteSpace(id)) return null;
        OUTL_FactionDef faction;
        return factionById.TryGetValue(id, out faction) ? faction : null;
    }

    public OUTL_QuestDef FindQuest(string id)
    {
        EnsureBuilt();
        if (string.IsNullOrWhiteSpace(id)) return null;
        OUTL_QuestDef quest;
        return questById.TryGetValue(id, out quest) ? quest : null;
    }

    public void RegisterQuests(OUTL_World world)
    {
        if (world == null || Quests == null) return;
        for (int i = 0; i < Quests.Length; i++)
            if (Quests[i] != null)
                world.Quests.AddQuest(Quests[i]);
    }

    private void AddEntities(OUTL_EntityDef[] defs)
    {
        if (defs == null) return;
        for (int i = 0; i < defs.Length; i++)
        {
            OUTL_EntityDef def = defs[i];
            if (def == null) continue;
            string id = def.GetDefId();
            if (!string.IsNullOrWhiteSpace(id)) entityById[id] = def;
            if (!string.IsNullOrWhiteSpace(def.name)) entityById[def.name] = def;
        }
    }

    private void EnsureBuilt()
    {
        if (!built) Rebuild();
    }
}
