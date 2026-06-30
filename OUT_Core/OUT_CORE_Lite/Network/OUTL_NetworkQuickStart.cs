using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_NetworkQuickStart : MonoBehaviour
{
    [TextArea(10, 24)]
    public string Notes =
        "OUT CORE Lite network quick start:\n" +
        "1. Install Mirror from the official source/package.\n" +
        "2. Add OUTL_MIRROR to Project Settings -> Player -> Scripting Define Symbols.\n" +
        "3. Create a Mirror NetworkManager and transport. For free local tests use LAN/direct IP first.\n" +
        "4. On player prefab add: OUTL_EntityAdapter, OUTL_NetworkIdentityLite, OUTL_MirrorEntityBridge, NetworkIdentity.\n" +
        "5. Put local-only scripts (player controller, camera/audio listener, input) into LocalOnlyBehaviours on OUTL_MirrorEntityBridge.\n" +
        "6. Host on one PC, client connects by IP. If NAT blocks internet testing, use LAN/VPN first; Steam relay can be added later.\n" +
        "7. Server should own OUTL_World state. Clients send commands, server replicates snapshots/deltas. Do not let clients load saves as authority.";
}
