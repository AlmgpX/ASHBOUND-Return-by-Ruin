using System.Numerics;
using Raylib_cs;
using OUT_RayMicro.Core;
using OUT_RayMicro.World;

namespace OUT_RayMicro.Gameplay;

public enum OutmPickupKind : byte
{
    Health,
    Armor,
    Mana
}

public struct OutmPickupRecord
{
    public EntityId Entity;
    public string Id;
    public OutmPickupKind Kind;
    public Vector3 Position;
    public float Radius;
    public int Amount;
    public OutmArmorTier ArmorTier;
    public bool Collected;
    public string SurfaceId;
}

public sealed class OutmPickupStore
{
    private readonly OutBuffer<OutmPickupRecord> pickups = new(128);
    private readonly Dictionary<string, int> byId = new(StringComparer.OrdinalIgnoreCase);

    public int Count => pickups.Count;
    public OutBuffer<OutmPickupRecord> Pickups => pickups;

    public void Add(EntityId entity, string id, OutmPickupKind kind, Vector3 position, float radius, int amount, OutmArmorTier armorTier, string surfaceId)
    {
        if (string.IsNullOrWhiteSpace(id))
            id = $"pickup.{pickups.Count}";

        var record = new OutmPickupRecord
        {
            Entity = entity,
            Id = id,
            Kind = kind,
            Position = position,
            Radius = MathF.Max(0.1f, radius),
            Amount = Math.Max(1, amount),
            ArmorTier = armorTier,
            Collected = false,
            SurfaceId = string.IsNullOrWhiteSpace(surfaceId) ? "surface.stone" : surfaceId
        };

        byId[id] = pickups.Count;
        pickups.Add(record);
    }

    public bool TrySetCollected(string id, bool collected)
    {
        if (!byId.TryGetValue(id, out int index))
            return false;

        OutmPickupRecord pickup = pickups.Items[index];
        pickup.Collected = collected;
        pickups.Items[index] = pickup;
        return true;
    }

    public void SetCollectedByIndex(int index, bool collected)
    {
        if ((uint)index >= (uint)pickups.Count)
            return;

        OutmPickupRecord pickup = pickups.Items[index];
        pickup.Collected = collected;
        pickups.Items[index] = pickup;
    }

    public bool TryGet(string id, out OutmPickupRecord pickup)
    {
        if (!string.IsNullOrWhiteSpace(id) && byId.TryGetValue(id, out int index))
        {
            pickup = pickups.Items[index];
            return true;
        }

        pickup = default;
        return false;
    }
}

public sealed class OutmPickupSystem
{
    public void Update(OutmWorld world, OutmPickupStore pickups, Vector3 playerPosition)
    {
        for (int i = 0; i < pickups.Count; i++)
        {
            OutmPickupRecord pickup = pickups.Pickups[i];
            if (pickup.Collected)
                continue;

            float radius = MathF.Max(0.1f, pickup.Radius);
            if (Vector3.DistanceSquared(playerPosition, pickup.Position) > radius * radius)
                continue;

            if (ApplyPickup(world, pickup))
                pickups.SetCollectedByIndex(i, true);
        }
    }

    public void Draw(OutmPickupStore pickups)
    {
        for (int i = 0; i < pickups.Count; i++)
        {
            OutmPickupRecord pickup = pickups.Pickups[i];
            if (pickup.Collected)
                continue;

            Color color = pickup.Kind switch
            {
                OutmPickupKind.Health => Color.Red,
                OutmPickupKind.Armor => Color.Green,
                OutmPickupKind.Mana => Color.Blue,
                _ => Color.White
            };

            Raylib.DrawSphere(pickup.Position, 0.22f, color);
            Raylib.DrawSphereWires(pickup.Position, pickup.Radius, 8, 8, new Color(255, 255, 255, 120));
        }
    }

    private static bool ApplyPickup(OutmWorld world, OutmPickupRecord pickup)
    {
        OutmPlayerVitals vitals = world.PlayerVitals;
        if (vitals.IsDead)
            return false;

        switch (pickup.Kind)
        {
            case OutmPickupKind.Health:
            {
                if (vitals.Health >= vitals.MaxHealth)
                    return false;

                int before = vitals.Health;
                vitals.Health = Math.Min(vitals.MaxHealth, vitals.Health + pickup.Amount);
                world.PlayerVitals = vitals;
                world.Emit(new OutmEvent(OutmEventType.ArmorPicked, pickup.Entity, world.PlayerEntity, pickup.Position, vitals.Health - before, $"pickup health {pickup.Amount}"));
                return true;
            }

            case OutmPickupKind.Mana:
            {
                if (vitals.Mana >= vitals.MaxMana)
                    return false;

                int before = vitals.Mana;
                vitals.Mana = Math.Min(vitals.MaxMana, vitals.Mana + pickup.Amount);
                world.PlayerVitals = vitals;
                world.Emit(new OutmEvent(OutmEventType.ArmorPicked, pickup.Entity, world.PlayerEntity, pickup.Position, vitals.Mana - before, $"pickup mana {pickup.Amount}"));
                return true;
            }

            case OutmPickupKind.Armor:
                return OutmDamageSystem.TryPickupQuakeArmor(world, pickup.ArmorTier, $"pickup armor {pickup.Id}");

            default:
                return false;
        }
    }
}
