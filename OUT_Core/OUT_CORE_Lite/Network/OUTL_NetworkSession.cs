using System;
using UnityEngine;

public enum OUTL_NetworkMode
{
    Offline = 0,
    Host = 1,
    Server = 2,
    Client = 3
}

[DisallowMultipleComponent]
public sealed class OUTL_NetworkSession : MonoBehaviour
{
    public static OUTL_NetworkSession ActiveSession { get; private set; }

    [Header("World Access")]
    public OUTL_World World;
    public OUTL_NetworkMode Mode = OUTL_NetworkMode.Offline;
    public bool WorldIsServerAuthority;
    public bool WorldIsClientReplica;

    [Header("Connection")]
    public string Address = "localhost";
    public int Port = 7777;
    public bool AutoFindWorld = true;
    public bool LogActions = true;

    public bool IsNetworkActive { get { return Mode == OUTL_NetworkMode.Host || Mode == OUTL_NetworkMode.Server || Mode == OUTL_NetworkMode.Client; } }
    public bool IsOffline { get { return Mode == OUTL_NetworkMode.Offline; } }
    public bool IsHostOrServer { get { return Mode == OUTL_NetworkMode.Host || Mode == OUTL_NetworkMode.Server; } }
    public bool IsClientOnly { get { return Mode == OUTL_NetworkMode.Client; } }

    private void Awake()
    {
        ActiveSession = this;
        if (World == null && AutoFindWorld) World = OUTL_World.Instance;
        RefreshModeFromTransport();
    }

    private void OnEnable()
    {
        ActiveSession = this;
    }

    private void OnDisable()
    {
        if (ActiveSession == this) ActiveSession = null;
    }

    private void Update()
    {
        RefreshModeFromTransport();
    }

    [ContextMenu("OUT Single Player Offline")]
    public void StartOfflineWorld()
    {
#if OUTL_MIRROR
        if (Mirror.NetworkServer.active || Mirror.NetworkClient.isConnected)
        {
            Mirror.NetworkManager manager = Mirror.NetworkManager.singleton;
            if (manager != null) manager.StopHost();
            else
            {
                if (Mirror.NetworkClient.isConnected) Mirror.NetworkClient.Disconnect();
                if (Mirror.NetworkServer.active) Mirror.NetworkServer.Shutdown();
            }
        }
#endif
        Mode = OUTL_NetworkMode.Offline;
        WorldIsServerAuthority = false;
        WorldIsClientReplica = false;
        Log("world access set to Offline. OUTL_World continues as local single-player world.");
    }

    [ContextMenu("OUT Open Current World As Host")]
    public bool OpenCurrentWorldAsHost()
    {
        EnsureWorld();
#if OUTL_MIRROR
        Mirror.NetworkManager manager = Mirror.NetworkManager.singleton;
        if (manager == null)
        {
            LogWarning("Cannot open world: Mirror NetworkManager is missing. Use OUT CORE Lite/Network/Create Open World Network Rig.");
            return false;
        }
        ApplyAddressAndPort(manager);
        if (!Mirror.NetworkServer.active && !Mirror.NetworkClient.isConnected)
            manager.StartHost();
        RefreshModeFromTransport();
        Log("opened current OUTL_World as Host on port " + Port + ". Same world, network access enabled.");
        return true;
#else
        LogWarning("Cannot open world: OUTL_MIRROR define is disabled or Mirror is not installed. Single-player world still works.");
        return false;
#endif
    }

    [ContextMenu("OUT Start Dedicated Server")]
    public bool StartDedicatedServer()
    {
        EnsureWorld();
#if OUTL_MIRROR
        Mirror.NetworkManager manager = Mirror.NetworkManager.singleton;
        if (manager == null)
        {
            LogWarning("Cannot start server: Mirror NetworkManager is missing.");
            return false;
        }
        ApplyAddressAndPort(manager);
        if (!Mirror.NetworkServer.active && !Mirror.NetworkClient.isConnected)
            manager.StartServer();
        RefreshModeFromTransport();
        Log("started OUTL_World as dedicated/listenless server on port " + Port + ".");
        return true;
#else
        LogWarning("Cannot start server: OUTL_MIRROR define is disabled or Mirror is not installed.");
        return false;
#endif
    }

    public bool JoinWorld(string address, int port)
    {
        Address = string.IsNullOrWhiteSpace(address) ? "localhost" : address.Trim();
        Port = Mathf.Clamp(port, 1, 65535);
        return JoinWorld();
    }

    [ContextMenu("OUT Join World")]
    public bool JoinWorld()
    {
#if OUTL_MIRROR
        Mirror.NetworkManager manager = Mirror.NetworkManager.singleton;
        if (manager == null)
        {
            LogWarning("Cannot join world: Mirror NetworkManager is missing.");
            return false;
        }
        ApplyAddressAndPort(manager);
        if (!Mirror.NetworkClient.isConnected && !Mirror.NetworkServer.active)
            manager.StartClient();
        RefreshModeFromTransport();
        Log("joining OUTL world at " + Address + ":" + Port + ". Local OUTL_World becomes client replica when connected.");
        return true;
#else
        LogWarning("Cannot join world: OUTL_MIRROR define is disabled or Mirror is not installed.");
        return false;
#endif
    }

    [ContextMenu("OUT Close Network Access")]
    public void CloseNetworkAccess()
    {
        StartOfflineWorld();
    }

    public void RefreshModeFromTransport()
    {
#if OUTL_MIRROR
        bool server = Mirror.NetworkServer.active;
        bool client = Mirror.NetworkClient.isConnected;
        if (server && client) Mode = OUTL_NetworkMode.Host;
        else if (server) Mode = OUTL_NetworkMode.Server;
        else if (client) Mode = OUTL_NetworkMode.Client;
        else Mode = OUTL_NetworkMode.Offline;
#else
        Mode = OUTL_NetworkMode.Offline;
#endif
        WorldIsServerAuthority = Mode == OUTL_NetworkMode.Host || Mode == OUTL_NetworkMode.Server || Mode == OUTL_NetworkMode.Offline;
        WorldIsClientReplica = Mode == OUTL_NetworkMode.Client;
    }

    private void EnsureWorld()
    {
        if (World == null && AutoFindWorld) World = OUTL_World.Instance;
    }

#if OUTL_MIRROR
    private void ApplyAddressAndPort(Mirror.NetworkManager manager)
    {
        manager.networkAddress = string.IsNullOrWhiteSpace(Address) ? "localhost" : Address.Trim();
        TrySetTransportPort(FindTransport(manager), Port);
    }

    private static Mirror.Transport FindTransport(Mirror.NetworkManager manager)
    {
        Mirror.Transport transport = manager != null ? manager.GetComponent<Mirror.Transport>() : null;
        if (transport != null) return transport;
        return null;
    }

    private static void TrySetTransportPort(Mirror.Transport transport, int port)
    {
        if (transport == null) return;
        Type type = transport.GetType();
        string[] names = { "Port", "port", "ServerPort", "serverPort" };
        for (int i = 0; i < names.Length; i++)
        {
            System.Reflection.PropertyInfo prop = type.GetProperty(names[i]);
            if (prop != null && prop.CanWrite && (prop.PropertyType == typeof(ushort) || prop.PropertyType == typeof(int)))
            {
                prop.SetValue(transport, prop.PropertyType == typeof(ushort) ? (object)(ushort)port : port, null);
                return;
            }
            System.Reflection.FieldInfo field = type.GetField(names[i]);
            if (field != null && (field.FieldType == typeof(ushort) || field.FieldType == typeof(int)))
            {
                field.SetValue(transport, field.FieldType == typeof(ushort) ? (object)(ushort)port : port);
                return;
            }
        }
    }
#endif

    private void Log(string message)
    {
        if (LogActions) Debug.Log("[OUTL NetworkSession] " + message, this);
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning("[OUTL NetworkSession] " + message, this);
    }
}
