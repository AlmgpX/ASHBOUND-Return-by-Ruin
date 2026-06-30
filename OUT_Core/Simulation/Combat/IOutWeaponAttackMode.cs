public interface IOutWeaponAttackMode
{
    bool CanFire(in OUT_WeaponFireContext context);
    void Fire(in OUT_WeaponFireContext context);
}