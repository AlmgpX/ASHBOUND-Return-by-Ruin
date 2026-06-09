using System;

namespace OUT_ASHBOUND;

public sealed class OUT_EffectSystem
{
    private readonly OUT_State state;

    public OUT_EffectSystem(OUT_State state)
    {
        this.state = state;
    }

    public void Hurt(int sourceId, int targetId, int amount)
    {
        var source = state.Table.Get(sourceId);
        var target = state.Table.Get(targetId);
        if (source == null || target == null) return;

        target.Stats.Hp = Math.Max(0, target.Stats.Hp - Math.Max(0, amount));
        state.Events.Emit(OUT_Event.Message("[SIGNAL] " + source.Def.Name + " strikes " + target.Def.Name + " for " + amount));

        if (target.Stats.Hp <= 0 && target.Id != state.PlayerId)
        {
            state.Events.Emit(OUT_Event.Message("[SHADOW] " + target.Def.Name + " falls"));
            state.Table.Drop(target.Id);
        }
    }

    public void ModifyHp(int id, int delta)
    {
        var obj = state.Table.Get(id);
        if (obj == null) return;
        obj.Stats.Hp = Math.Clamp(obj.Stats.Hp + delta, 0, obj.Stats.MaxHp);
    }

    public void ModifyStamina(int id, int delta)
    {
        var obj = state.Table.Get(id);
        if (obj == null) return;
        obj.Stats.Stamina = Math.Clamp(obj.Stats.Stamina + delta, 0, obj.Stats.MaxStamina);
    }
}
