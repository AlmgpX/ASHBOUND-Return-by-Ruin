using UnityEngine;

[DisallowMultipleComponent]
public class OUT_HitscanAttackModeAdapter : MonoBehaviour, IOutAttackMode
{
    private readonly OUT_HitscanAttackMode _mode = new OUT_HitscanAttackMode();

    public void Execute(in OUT_AttackContext context)
    {
        _mode.Execute(context);
    }
}
