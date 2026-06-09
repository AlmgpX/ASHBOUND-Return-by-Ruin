using System.Collections.Generic;

namespace OUT_ASHBOUND;

public sealed class OUT_RuntimeObject
{
    public int Id { get; }
    public OUT_EntityDef Def { get; }
    public OUT_Pos Pos { get; set; }
    public OUT_Pos WorldPos { get; set; }
    public OUT_Scope Scope { get; set; }
    public OUT_Stats Stats { get; }
    public string TargetName { get; set; }
    public string ClassName { get; set; }
    public Dictionary<string, int> Bag { get; } = new();

    public OUT_RuntimeObject(int id, OUT_EntityDef def, OUT_Pos pos, OUT_Scope scope)
    {
        Id = id;
        Def = def;
        Pos = pos;
        WorldPos = pos;
        Scope = scope;
        Stats = new OUT_Stats(def.Hp, def.Stamina, def.Attack);
        TargetName = def.Key + "." + id;
        ClassName = def.ClassName;
    }

    public void OUT_Put(string key, int count)
    {
        if (string.IsNullOrWhiteSpace(key) || count <= 0) return;
        Bag[key] = Bag.TryGetValue(key, out int old) ? old + count : count;
    }
}
