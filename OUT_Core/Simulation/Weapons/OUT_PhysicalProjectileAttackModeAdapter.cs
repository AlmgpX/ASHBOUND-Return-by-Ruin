using UnityEngine;

[DisallowMultipleComponent]
public class OUT_PhysicalProjectileAttackModeAdapter : MonoBehaviour, IOutAttackMode
{
    private readonly OUT_PhysicalProjectileAttackMode _mode = new OUT_PhysicalProjectileAttackMode();

    public void Execute(in OUT_AttackContext context)
    {
        _mode.Execute(context);
    }
}
