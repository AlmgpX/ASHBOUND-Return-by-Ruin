using UnityEngine;

[DisallowMultipleComponent]
public sealed class OUTL_NetworkLanMenu : MonoBehaviour
{
    public OUTL_NetworkSession Session;
    public bool ShowMenu = true;
    public KeyCode ToggleKey = KeyCode.F1;
    public string Address = "localhost";
    public int Port = 7777;
    public Rect WindowRect = new Rect(20f, 80f, 390f, 300f);

    private void Awake()
    {
        if (Session == null) Session = GetComponent<OUTL_NetworkSession>();
        if (Session != null)
        {
            Address = Session.Address;
            Port = Session.Port;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) ShowMenu = !ShowMenu;
        if (Session != null) Session.RefreshModeFromTransport();
    }

    private void OnGUI()
    {
        if (!ShowMenu) return;
        WindowRect = GUI.Window(GetInstanceID(), WindowRect, DrawWindow, "OUTL World Access");
    }

    private void DrawWindow(int id)
    {
        if (Session == null)
        {
            Session = GetComponent<OUTL_NetworkSession>();
        }

        if (Session == null)
        {
            GUILayout.Label("No OUTL_NetworkSession in scene.");
            GUILayout.Label("Use OUT CORE Lite/Network/Create Open World Network Rig.");
            GUI.DragWindow();
            return;
        }

        GUILayout.Label("World Mode: " + Session.Mode);
        GUILayout.Label(Session.WorldIsClientReplica ? "This client should receive server authority." : "This world is local/server authority.");
        GUILayout.Space(4f);

        GUILayout.Label("Friend / Server Address");
        Address = GUILayout.TextField(Address);
        GUILayout.Label("Port");
        string portText = GUILayout.TextField(Port.ToString());
        int parsedPort;
        if (int.TryParse(portText, out parsedPort)) Port = Mathf.Clamp(parsedPort, 1, 65535);
        Session.Address = Address;
        Session.Port = Port;

        GUILayout.Space(8f);
        GUI.enabled = Session.Mode == OUTL_NetworkMode.Offline;
        if (GUILayout.Button("Open Current World To Friends (Host)"))
        {
            Session.Address = Address;
            Session.Port = Port;
            Session.OpenCurrentWorldAsHost();
        }

        if (GUILayout.Button("Join Friend World"))
        {
            Session.JoinWorld(Address, Port);
        }
        GUI.enabled = true;

        GUILayout.BeginHorizontal();
        GUI.enabled = Session.Mode == OUTL_NetworkMode.Offline;
        if (GUILayout.Button("Server Only")) Session.StartDedicatedServer();
        GUI.enabled = Session.Mode != OUTL_NetworkMode.Offline;
        if (GUILayout.Button("Close Network Access")) Session.CloseNetworkAccess();
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);
        GUILayout.Label("Single-player and host use the same OUTL_World.");
        GUILayout.Label("Network is an access layer, not a second world.");
        GUILayout.Label("F1 toggles this menu.");
        GUILayout.Space(6f);
        GUILayout.Label("Internet direct join needs reachable host port unless a relay/Steam adapter is added later.");
        GUI.DragWindow();
    }
}
