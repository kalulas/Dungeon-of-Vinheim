using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace DungeonOfVinheim{
    public class Launcher : MonoBehaviourPunCallbacks
    {
        public string gameVersion { get; set; } = "0.1";
        public byte mapSize { get; set; } = 5;
        [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
        [SerializeField]
        private byte maxPlayersPerRoom = 4;

        private bool isConnecting = false;
        [Tooltip("The Ui Panel to let the user enter name, connect and play")]
        [SerializeField]
        private GameObject controlPanel;
        [Tooltip("The UI Label to inform the user that the connection is in progress")]
        [SerializeField]
        private GameObject progressLabel;

        private bool ConnectToChina()
        {
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
        void Start()
        {
            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
            // Connect();
        }

        public override void OnConnectedToMaster(){
            Debug.Log("Connected to Master!");
            if (isConnecting)
            {
                // #Critical: The first we try to do is to join a potential existing room. If there is, good, else, we'll be called back with OnJoinRandomFailed()
                PhotonNetwork.JoinRandomRoom();
                isConnecting = false;
            }
        }

        public override void OnDisconnected(DisconnectCause cause){
            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
            Debug.LogWarningFormat("OnDisconnected() was called by PUN with reason {0}", cause);
        }

        public override void OnJoinRandomFailed(short returnCode, string message){
            Debug.Log("No random room available,so we create one.\nCalling: PhotonNetwork.CreateRoom");
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
        }

        public override void OnJoinedRoom(){
            Debug.Log("Now this client is in a room.");
            PhotonNetwork.LoadLevel("Dungeon");
        }
        /// <summary>
        /// Start the connection process.
        /// - If already connected, we attempt joining a random room
        /// - if not yet connected, Connect this application instance to Photon Cloud Network
        /// </summary>
        public void Connect()
        {
            progressLabel.SetActive(true);
            controlPanel.SetActive(false);
            // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("IsConnected,join random");
                // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                Debug.Log("IsNotConnected,join connet using settings");
                // #Critical, we must first and foremost connect to Photon Online Server.
                // isConnecting = PhotonNetwork.ConnectUsingSettings();
                isConnecting = ConnectToChina();
                PhotonNetwork.GameVersion = gameVersion;
            }
        }

        public void Exit(){
            Application.Quit();
        }

    }
}

