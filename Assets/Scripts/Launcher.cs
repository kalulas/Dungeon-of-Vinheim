using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class Launcher : MonoBehaviourPunCallbacks {
    public string gameVersion { get; set; } = "0.1";
    public int mapSize = 5;
    [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
    [SerializeField]
    private readonly byte maxPlayersPerRoom = 5;

    private bool isConnecting = false;
    [Tooltip("The Ui Panel to let the user enter name, connect and play")]
    [SerializeField]
    private GameObject controlPanel;
    [Tooltip("The UI Label to inform the user that the connection is in progress")]
    [SerializeField]
    private GameObject progressLabel;

    private bool ConnectToChina() {
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "cn";
        PhotonNetwork.PhotonServerSettings.AppSettings.UseNameServer = true;
        PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = "cc38a4a7-6754-48ce-aa58-637dadeaefaf"; // 替换为您自己的国内区appID
        PhotonNetwork.PhotonServerSettings.AppSettings.Server = "ns.photonengine.cn";
        return PhotonNetwork.ConnectUsingSettings();
    }

    private void Awake() {
        // #Critical
        // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    // Start is called before the first frame update
    void Start() {
        progressLabel.SetActive(false);
        controlPanel.SetActive(true);
        AClockworkBerry.ScreenLogger.CreateInstance();
        // Connect();
    }

    public override void OnConnectedToMaster() {
        Debug.Log("Connected to Master!");
        if (isConnecting) {
            // #Critical: The first we try to do is to join a potential existing room. If there is, good, else, we'll be called back with OnJoinRandomFailed()
            Hashtable filter = new Hashtable() {
                {RoomPropManager.PropKeys[RoomPropType.GameStart], false },
            };
            PhotonNetwork.JoinRandomRoom(filter, maxPlayersPerRoom);
            //PhotonNetwork.JoinRandomRoom();
            isConnecting = false;
        }
    }

    public override void OnDisconnected(DisconnectCause cause) {
        if (progressLabel) progressLabel.SetActive(false);
        if (controlPanel) controlPanel.SetActive(true);
        Debug.LogWarningFormat("OnDisconnected() was called by PUN with reason {0}", cause);
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        Debug.Log("No random room available, create a new room");
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayersPerRoom;
        roomOptions.CustomRoomProperties = new Hashtable() {
            {RoomPropManager.PropKeys[RoomPropType.GameStart], false },
            {RoomPropManager.PropKeys[RoomPropType.MapSize], mapSize },
            {RoomPropManager.PropKeys[RoomPropType.Duration], 300 },
            {RoomPropManager.PropKeys[RoomPropType.WaitDown], 0 },
            {RoomPropManager.PropKeys[RoomPropType.WaitUp], 0 },
            {RoomPropManager.PropKeys[RoomPropType.WaitLeft], 0 },
            {RoomPropManager.PropKeys[RoomPropType.WaitRight], 0 },
        };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { RoomPropManager.PropKeys[RoomPropType.GameStart] };
        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnJoinedRoom() {
        Debug.Log("Now this client is in a room.");
        PhotonNetwork.LoadLevel("Dungeon");
    }
    /// <summary>
    /// Start the connection process.
    /// - If already connected, we attempt joining a random room
    /// - if not yet connected, Connect this application instance to Photon Cloud Network
    /// </summary>
    public void Connect() {
        progressLabel.SetActive(true);
        controlPanel.SetActive(false);
        // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
        if (PhotonNetwork.IsConnected) {
            Debug.Log("Connected,join random");
            // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
            PhotonNetwork.JoinRandomRoom();
        } else {
            Debug.Log("Not Connected, connect to Photon Online Server");
            // #Critical, we must first and foremost connect to Photon Online Server.
            // isConnecting = PhotonNetwork.ConnectUsingSettings();
            isConnecting = ConnectToChina();
            PhotonNetwork.GameVersion = gameVersion;
        }
    }

    public void Exit() {
        Application.Quit();
    }

}

