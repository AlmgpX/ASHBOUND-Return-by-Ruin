using UnityEngine;

public static class OUTL_PlayerDeathExtensions
{
    public static void SetOUTLDead(this OUTL_BasicPlayerController controller, bool dead)
    {
        if (controller == null) return;
        controller.enabled = !dead;
        CharacterController cc = controller.GetComponent<CharacterController>();
        if (cc != null && cc.enabled && dead) cc.Move(Vector3.zero);
    }
}
