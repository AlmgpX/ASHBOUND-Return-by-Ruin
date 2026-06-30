using UnityEngine;

#if OUTL_MIRROR
using Mirror;

public sealed partial class OUTL_MirrorEntityBridge : NetworkBehaviour { }
#else
public sealed partial class OUTL_MirrorEntityBridge : MonoBehaviour { }
#endif
