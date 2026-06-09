using System;
using System.Collections.Generic;
using System.Linq;

namespace OUT_ASHBOUND;

public sealed class OUT_Table
{
    private int nextId = 1;
    private readonly Dictionary<int, OUT_RuntimeObject> rows = new();
    private readonly List<OUT_RuntimeObject> list = new();

    public IReadOnlyList<OUT_RuntimeObject> All => list;

    public int Create(OUT_EntityDef def, OUT_Pos pos, OUT_Scope scope)
    {
        int id = nextId++;
        var obj = new OUT_RuntimeObject(id, def, pos, scope);
        rows[id] = obj;
        list.Add(obj);
        return id;
    }

    public void AddExisting(OUT_RuntimeObject obj)
    {
        rows[obj.Id] = obj;
        list.Add(obj);
        nextId = Math.Max(nextId, obj.Id + 1);
    }

    public OUT_RuntimeObject? Get(int id) => rows.TryGetValue(id, out var obj) ? obj : null;

    public void Drop(int id)
    {
        if (!rows.TryGetValue(id, out var obj)) return;
        rows.Remove(id);
        list.Remove(obj);
    }

    public void DropScope(OUT_Scope scope)
    {
        foreach (var obj in list.ToList())
            if (obj.Scope == scope && obj.Def.Key != "player") Drop(obj.Id);
    }

    public OUT_RuntimeObject? BlockingAt(OUT_Pos pos, OUT_Scope scope)
    {
        return list.FirstOrDefault(x => x.Scope == scope && x.Pos == pos && x.Def.Blocking && x.Stats.Hp > 0);
    }

    public OUT_RuntimeObject? FirstAt(OUT_Pos pos, OUT_Scope scope, bool includeActor)
    {
        return list.FirstOrDefault(x => x.Scope == scope && x.Pos == pos && (includeActor || !x.Def.IsActor));
    }
}
