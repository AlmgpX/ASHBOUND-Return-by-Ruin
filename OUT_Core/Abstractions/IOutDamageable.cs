public interface IOutDamageable
{
    bool CanTakeDamage(in OUT_DamageContext context);
    void ApplyDamage(in OUT_DamageContext context);
}
