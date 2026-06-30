public enum OUTL_StatId : byte
{
    Health = 0,
    Damage = 1,
    Speed = 2,
    Stamina = 3,
    Mana = 4,
    Armor = 5,
    Count = 6,
    None = 255
}

public enum OUTL_StateId : byte
{
    Open = 0,
    On = 1,
    Dead = 2,
    Alert = 3,
    Combat = 4,
    Locked = 5,
    Count = 6,
    None = 255
}

public static class OUTL_CompactIds
{
    public static OUTL_StatId StatFromKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return OUTL_StatId.None;
        switch (key)
        {
            case "Health": case "health": case "hp": case "HP": return OUTL_StatId.Health;
            case "Damage": case "damage": case "dmg": case "DMG": return OUTL_StatId.Damage;
            case "Speed": case "speed": return OUTL_StatId.Speed;
            case "Stamina": case "stamina": return OUTL_StatId.Stamina;
            case "Mana": case "mana": return OUTL_StatId.Mana;
            case "Armor": case "armor": return OUTL_StatId.Armor;
            default: return OUTL_StatId.None;
        }
    }

    public static string StatToKey(OUTL_StatId id)
    {
        switch (id)
        {
            case OUTL_StatId.Health: return "Health";
            case OUTL_StatId.Damage: return "Damage";
            case OUTL_StatId.Speed: return "Speed";
            case OUTL_StatId.Stamina: return "Stamina";
            case OUTL_StatId.Mana: return "Mana";
            case OUTL_StatId.Armor: return "Armor";
            default: return string.Empty;
        }
    }

    public static OUTL_StateId StateFromKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return OUTL_StateId.None;
        switch (key)
        {
            case "Open": case "open": return OUTL_StateId.Open;
            case "On": case "on": return OUTL_StateId.On;
            case "Dead": case "dead": return OUTL_StateId.Dead;
            case "Alert": case "alert": return OUTL_StateId.Alert;
            case "Combat": case "combat": return OUTL_StateId.Combat;
            case "Locked": case "locked": return OUTL_StateId.Locked;
            default: return OUTL_StateId.None;
        }
    }

    public static string StateToKey(OUTL_StateId id)
    {
        switch (id)
        {
            case OUTL_StateId.Open: return "Open";
            case OUTL_StateId.On: return "On";
            case OUTL_StateId.Dead: return "Dead";
            case OUTL_StateId.Alert: return "Alert";
            case OUTL_StateId.Combat: return "Combat";
            case OUTL_StateId.Locked: return "Locked";
            default: return string.Empty;
        }
    }
}
