using UnityEngine;

[DisallowMultipleComponent]
public class OUTL_DamageReceiver : MonoBehaviour, OUTL_ICommandReceiver
{
    public OUTL_EntityAdapter Entity;
    public float DefaultDamage = 10f;

    private void Awake()
    {
        if (Entity == null) Entity = GetComponent<OUTL_EntityAdapter>();
    }

    public bool OUTL_CanReceive(in OUTL_Command command, OUTL_World world)
    {
        return Entity != null && Entity.Runtime != null && (command.Type == OUTL_CommandType.Damage || command.Type == OUTL_CommandType.Attack);
    }

    public void OUTL_Receive(in OUTL_Command command, OUTL_World world)
    {
        float damage = command.FloatValue > 0f ? command.FloatValue : (command.IntValue > 0 ? command.IntValue : DefaultDamage);
        OUTL_Combat.ApplyDamage(command.Source, Entity.Id, damage, command.Point != Vector3.zero ? command.Point : transform.position);
    }
}
