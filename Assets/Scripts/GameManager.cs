using System.Collections;
using System.Collections.Generic;
using Invector;
using Invector.vCharacterController;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using Photon.Pun;
using Photon.Realtime;

public static class IListExtensions
{
    /// <summary>
    /// Shuffles the element order of the specified list. [start, end)
    /// </summary>
    public static void Shuffle<T>(this IList<T> ts, int start, int end)
    {
        // var count = ts.Count;
        // var last = count - 1;
        for (int i = start; i < end; ++i)
        {
            int r = UnityEngine.Random.Range(i, end);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}

public class TriggerEvent : UnityEvent{ }

namespace DungeonOfVinheim
{
    using ExitGames.Client.Photon;
    // 单例脚本管理地图信息并进行关卡切换
    public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public static GameManager instance;
        public static TriggerEvent ActionTriggerEvent = new TriggerEvent();
        /// <summary>
        /// positions stored for player's reposition when he enters a new room
        /// </summary>
        public static Vector3[] positions = new Vector3[5] {
            new Vector3 (15.4f, 0.15f, 0f), // down 
            new Vector3 (-1.3f, 0.15f, 16.66f), // left
            new Vector3 (32.22f, 0.15f, 16.73f), // right
            new Vector3 (15.36f, 0.15f, 33.23f), // up
            new Vector3 (15.45f, 0.15f, 16.66f) //center
        };
        public static readonly string playerLocationKey = "roomNumber";
        public static readonly string mapDataKey = "gridMap";
        public static readonly string enemyListKey = "enemyList";
        public static readonly string obstaclePathKey = "obstaclePath";
        public static readonly string obstaclePosKey = "obstaclePos";

        public const byte JustEnterRoom = 1;
        public const byte StartEnterRoomCountDown = 2;
        public const byte CancelEnterRoomCountDown = 3;

        // the same as original vThirdPersonController.LocalPlayerInstance
        [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
        public static GameObject localPlayerInstance;
        public List<GameObject> otherPlayers = new List<GameObject>();

        // patrol area for enemies
        public GameObject WaypointArea;
        public GameObject enemiesSpawnPointsContainer;
        // transform data for enemies' spawning
        public List<Transform> enemiesSpawnPoints = new List<Transform>();
        // transform data for obstacles' spawning
        public List<Transform> obstaclesSpawnPoints = new List<Transform>();

        public int mapSize { get; private set; } = 5;
        public int emptyRoomNumber { get; private set; }
        public int roomNumberLocal { get; private set; }

        private float entranceWaitTime = 5.0f;
        private bool entranceAvailable = true;
        private bool entranceWaiting = false;
        private Coroutine entranceCountDownCor = null;
        private Coroutine entranceEnterCor = null;
        // basic environment for all rooms
        private GameObject defaultRoom;
        private List<Room> rooms = new List<Room>();

        #region Photon Callbacks
        /// <summary>
        /// Called when the local player left the room. We need to load the launcher scene.
        /// </summary>
        public override void OnLeftRoom()
        {
            SceneManager.LoadScene(0);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer){
            Debug.LogFormat("OnPlayerEnteredRoom() {0}",newPlayer.NickName); // not seen if you're the player connecting
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.LogFormat("OnPlayerEnteredRoom(): update {0}'s map", newPlayer); // called before OnPlayerLeftRoom
            }
        }

        public void OnEvent(EventData photonEvent)
        {
            byte eventCode = photonEvent.Code;
            switch (eventCode)
            {
                case JustEnterRoom: 
                    object[] data0 = (object[])photonEvent.CustomData;
                    UIManager.instance.SetCountDownText("");
                    Direction dir0 = (Direction)data0[0]; EnterRoom(dir0); 
                    break;
                case StartEnterRoomCountDown:
                    object[] data1 = (object[])photonEvent.CustomData;
                    Direction dir1 = (Direction)data1[0];
                    Debug.Log("start wait in direction " + dir1);
                    float countDown = entranceWaitTime;
                    entranceCountDownCor = WaitTimeManager.CreateCoroutine(true, entranceWaitTime, delegate ()
                    {
                        countDown -= 1.0f;
                        UIManager.instance.SetCountDownText(countDown.ToString());
                    }, 1.0f, true);
                    entranceEnterCor = WaitTimeManager.CreateCoroutine(false, entranceWaitTime, delegate ()
                    {
                        UIManager.instance.SetCountDownText("");
                        EnterRoom(dir1);
                    });
                    break;
                case CancelEnterRoomCountDown: 
                    object[] data2 = (object[])photonEvent.CustomData;
                    Direction dir2 = (Direction)data2[0];
                    WaitTimeManager.CancelCoroutine(ref entranceEnterCor);
                    WaitTimeManager.CancelCoroutine(ref entranceCountDownCor);
                    UIManager.instance.SetCountDownText("");
                    Debug.Log("cancel wait in direction " + dir2 + " if waited");
                    break;
                default: break;
            }
        }

        public override void OnPlayerLeftRoom(Player newPlayer)
        {
            Debug.LogFormat("OnPlayerLeftRoom() {0}", newPlayer.NickName); // seen when other disconnects    
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", newPlayer.IsMasterClient); // called before OnPlayerLeftRoom
            }
        }

        // NOTE: player's properties change event can only caught by the same photonview
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps){
            // Debug.Log("OnPlayerPropertiesUpdate()");
            // foreach (GameObject player in otherPlayers)
            // {
            //     if(targetPlayer == player.GetComponent<PhotonView>().Owner){
            //         ExitGames.Client.Photon.Hashtable userProperty = targetPlayer.CustomProperties;
            //         // [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
            //         int roomNumber = (int)userProperty[playerLocationKey];
            //         Debug.LogFormat("OnPlayerPropertiesUpdate() Player {0} has entered room {1}",targetPlayer, roomNumber);
            //         player.SetActive(roomNumber == roomNumberLocal);
            //     }
            // }
            
        }

        #endregion

        // IF CROSS SCENE
        // static GameManager(){
        //     GameObject gm = new GameObject("#GameManager#");
        //     DontDestroyOnLoad(gm);
        //     instance = gm.AddComponent<GameManager>();
        // }

        public RoomType GetRoomTypeByIndex(int idx)
        {
            return rooms[idx].roomType;
        }

        public new void OnEnable()
        {
            base.OnEnable();
            PhotonNetwork.AddCallbackTarget(this);
        }

        public new void OnDisable()
        {
            base.OnDisable();
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        public void BuildAndCreateMap(object[] roomtypes){
            int roomNumber = 0;
            GameObject startingRoomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/RoomExtras/Starting_Room_Extra"));
            GameObject rewardRoomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/RoomExtras/Reward_Room_Extra"));
            GameObject battleRoomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/RoomExtras/Battle_Room_Extra"));
            GameObject bossRoomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/RoomExtras/Boss_Room_Extra"));
            for (int i = 0; i < mapSize * mapSize; i++)
            {
                RoomType rt = (RoomType)roomtypes[i];
                switch (rt)
                {
                    case RoomType.EmptyRoom:
                    rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.EmptyRoom));
                    emptyRoomNumber = i;
                    break;
                    case RoomType.StartingRoom:
                    rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.StartingRoom, startingRoomExtra));
                    defaultRoom.GetComponent<NavMeshSurface>().BuildNavMesh();
                    break;
                    case RoomType.BattleRoom:
                    enemiesSpawnPoints.Shuffle(0, enemiesSpawnPoints.Count);
                    enemiesSpawnPointsContainer.GetComponentsInChildren<Transform>(true, enemiesSpawnPoints);
                    GameObject randomObstacles = GameObject.Find("Random Obstacles");
                    randomObstacles.GetComponentsInChildren<Transform>(true, obstaclesSpawnPoints);
                    rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.BattleRoom, battleRoomExtra));
                    break;
                    case RoomType.RewardRoom:
                    rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.RewardRoom, rewardRoomExtra));
                    break;
                    case RoomType.BossRoom:
                    rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.BossRoom, bossRoomExtra));
                    break;
                    default:break;
                }
            }
            battleRoomExtra.SetActive(false);
            rewardRoomExtra.SetActive(false);
            bossRoomExtra.SetActive(false);
            UIManager.instance.DrawMinimap();
            
             // load & justify player position
            if(localPlayerInstance == null){
                Debug.LogFormat("We are Instantiating LocalPlayer from {0}", Application.loadedLevelName);
                localPlayerInstance = PhotonNetwork.Instantiate("Prefabs/Players/Knight_Male_Player", positions[4], Quaternion.identity, 0);
            }

            // let others devices disactive me
            // Hashtable userProperty = new Hashtable();
            // userProperty[playerLocationKey] = roomNumberLocal;
            // PhotonNetwork.LocalPlayer.SetCustomProperties(userProperty);
            // disactive others
            // DisplayPlayersInCurrentRoom();

        }

        // Start is called before the first frame update
        void Awake()
        {
            object[] roomtypes = new object[mapSize * mapSize];
            // now game manager is attached to gameobject 'Game'
            instance = this;
            // Debug.Log("instance of 'Game Manager' generated.");
            roomNumberLocal = 0;

            if (PhotonNetwork.IsMasterClient)
            {
                int index = 0;
                roomtypes[index++] = RoomType.StartingRoom;
                roomtypes[index++] = RoomType.EmptyRoom;
                for (; index < mapSize * mapSize / 4 + 2;) roomtypes[index++] = RoomType.RewardRoom;
                while (index != mapSize * mapSize - 1) roomtypes[index++] = RoomType.BattleRoom;
                roomtypes[index] = RoomType.BossRoom;
                roomtypes.Shuffle(1, mapSize * mapSize - 1);

                Hashtable roomProperties = new Hashtable(){ 
                    { mapDataKey, roomtypes },
                    {"Down",0},
                    {"Up",0},
                    {"Left",0},
                    {"Right",0},
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
            }

            // Add Environment(like entrances) Trigger Listener
            // ⬇ also available
            // triggerEventStay.AddListener((Collider collider, GameObject item)=>{
            // ItemTrigger.triggerEventStay.AddListener(delegate (Collider collider, GameObject item)
            // {
            //     if (Input.GetButtonDown("A"))
            //     {
            //         if (ItemTrigger.triggerEventStay.type == TriggerEventType.Entrance)
            //         {
            //             HandleEntranceEvent(ItemTrigger.triggerEventStay.direction);
            //             // else can be locked by the host
            //         }
            //     }
            // });

        }

        private void Start() {
            Room.roomsContainer = new GameObject("rooms");
            defaultRoom = GameObject.Find("Default Room");
            Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
            object[] gridMap = (object[])roomProperties[mapDataKey];
            BuildAndCreateMap(gridMap);
        }

        // room can only be moved to the empty room's position
        // triggered on Move Button Down
        public void MasterMoveRoom(int roomNumber)
        {
            Room T = rooms[emptyRoomNumber];
            rooms[emptyRoomNumber] = rooms[roomNumber];
            rooms[roomNumber] = T;
            // update empty room number
            emptyRoomNumber = roomNumber;
        }

        /// <summary>display players in current room and hide those are not</summary>
        private void DisplayPlayersInCurrentRoom(){
            foreach (GameObject player in otherPlayers)
            {
                if(player){
                    Hashtable ht = player.GetComponent<PhotonView>().Owner.CustomProperties;
                    player.SetActive((int)ht[playerLocationKey] == roomNumberLocal);
                }
            }
        }

        /// <summary>
        /// some points in waypoint area might not be available because of the randomly generated obstacles
        /// so check every point in the area and reset their variables
        /// </summary>
        private void WaypointsValidation()
        {
            vPoint[] points = WaypointArea.GetComponentsInChildren<vPoint>();
            foreach (var point in points)
            {
                point.SetValidIfInNavMesh();
            }
        }

        private int GetPace(Direction direction){
            int pace;
            switch (direction)
            {
                // check if the room is on the edge
                case Direction.Down:
                    pace = roomNumberLocal / mapSize == 0 ? 0 : -mapSize;
                    break;
                case Direction.Left:
                    pace = roomNumberLocal % mapSize == 0 ? 0 : -1;
                    break;
                case Direction.Right:
                    pace = roomNumberLocal % mapSize == mapSize - 1 ? 0 : 1;
                    break;
                case Direction.Up:
                    pace = roomNumberLocal / mapSize == mapSize - 1 ? 0 : mapSize;
                    break;
                default:
                    pace = 0;
                    break;
            }
            return pace;
        }

        /// <summary>
        /// handle triggerevent when the player is trying to enter the next room
        /// </summary>
        public void HandleEntranceEvent(Direction direction)
        {
            int pace = GetPace(direction);
            if (pace == 0 || rooms[roomNumberLocal + pace].Empty())
            {
                Debug.Log("BLOCKED");
                // notify player that no room ahead, also no need for prefix "press e to"
                UIManager.setActionTextContentEvent.Invoke("BLOCKED", false);
            }
            else if(entranceAvailable)
            {
                // prevent from player's pressing too many times
                // entranceAvailable = false;
                Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; 
                SendOptions sendOptions = new SendOptions { Reliability = true };
                object[] content = new object[1];
                int playerInRoom = PhotonNetwork.CurrentRoom.PlayerCount;
                Direction[] dirs = new Direction[] { Direction.Down, Direction.Left, Direction.Right, Direction.Up };

                localPlayerInstance.SendMessage("SetLockAllInput", !entranceWaiting);

                if(!entranceWaiting){
                    // logic of entrance's queue : press E first time
                    entranceWaiting = true;

                    roomProperties[direction.ToString()] = (int)roomProperties[direction.ToString()] + 1;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
                    
                    bool waitBalance = false;
                    Direction originalDir = Direction.Down;
                    
                    foreach (Direction dir in dirs)
                    {
                        if ((int)roomProperties[dir.ToString()] == (int)roomProperties[direction.ToString()] && dir != direction) {
                            if ((int)roomProperties[dir.ToString()] == Mathf.Ceil((playerInRoom - 1) / 2))
                            { originalDir = dir; waitBalance = true; break; }
                        }
                    }
                    // same players at different entrance
                    if(waitBalance){
                        content[0] = originalDir;
                        PhotonNetwork.RaiseEvent(CancelEnterRoomCountDown, content, raiseEventOptions, sendOptions);
                        // cancel count down event with originalDir
                    }
                    else if( playerInRoom == 1 || (int)roomProperties[direction.ToString()] == playerInRoom-1) {
                        content[0] = direction;
                        PhotonNetwork.RaiseEvent(CancelEnterRoomCountDown, content, raiseEventOptions, sendOptions);
                        PhotonNetwork.RaiseEvent(JustEnterRoom, content, raiseEventOptions, sendOptions);
                        // cancel count down event with direction
                        // invoke enter room event with direction
                    }
                    else if((int)roomProperties[direction.ToString()] == Mathf.Ceil((playerInRoom-1)/2)){
                        content[0] = direction;
                        PhotonNetwork.RaiseEvent(StartEnterRoomCountDown, content, raiseEventOptions, sendOptions);
                    }

                }
                else{
                    // logic of entrance's queue : press E second time
                    entranceWaiting = false;
                    
                    roomProperties[direction.ToString()] = (int)roomProperties[direction.ToString()] - 1;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

                    bool waitUnbalance = false;
                    Direction anotherDir = Direction.Down;

                    foreach (Direction dir in dirs)
                    {
                        if ((int)roomProperties[dir.ToString()] > Mathf.Ceil((playerInRoom - 1) / 2)) { 
                            waitUnbalance = true; anotherDir = dir; break; 
                        }
                    }
                    if(waitUnbalance){
                        content[0] = anotherDir;
                        PhotonNetwork.RaiseEvent(StartEnterRoomCountDown, content, raiseEventOptions, sendOptions);
                        // invoke count down event with anotherDir
                    }
                    else if((int)roomProperties[direction.ToString()] < Mathf.Ceil((playerInRoom-1)/2)){
                        content[0] = direction;
                        PhotonNetwork.RaiseEvent(CancelEnterRoomCountDown, content, raiseEventOptions, sendOptions);
                        // cancel count down event
                    }
                    
                }

                // EnterRoom(entrance, direction);
            }
            else{
                Debug.Log("sorry entrance not available");
            }
        }

        private void EnterRoom(Direction direction){
            // same network events may arrive during the process
            entranceAvailable = false;
            // some item on exit cant function properly
            ActionTriggerEvent.RemoveAllListeners();
            GameObject entrance = GameObject.Find("Entrance" + direction.ToString());

            int pace = GetPace(direction);
            // NOTE: no TRIGGER EXIT event now so manually set action text false
            UIManager.setActionTextActiveEvent.Invoke(false);
            // TODO: play smoke animation & player's open door animation maybe you need to reposition the player
            Animation ani = entrance.GetComponent<Animation>();
            ani.Play("DoorOpen");
            // wait until the entrance is fully open
            ItemTrigger.entranceFullyOpenEvent.AddListener(delegate ()
            {
                rooms[roomNumberLocal].SetRoomEnvActive(false);
                rooms[roomNumberLocal].SetRoomObjectsActive(false);
                // update currentLocation
                roomNumberLocal += pace;
                // change player's position (Tranform component)
                switch (direction)
                {
                    // check if the room is on the edge
                    case Direction.Down:
                        localPlayerInstance.GetComponent<Transform>().position = positions[3];
                        break;
                    case Direction.Left:
                        localPlayerInstance.GetComponent<Transform>().position = positions[2];
                        break;
                    case Direction.Right:
                        localPlayerInstance.GetComponent<Transform>().position = positions[1];
                        break;
                    case Direction.Up:
                        localPlayerInstance.GetComponent<Transform>().position = positions[0];
                        break;
                    default:
                        break;
                }
                // check otherplayers' roomNumber before the new room activated
                // DisplayPlayersInCurrentRoom();
                rooms[roomNumberLocal].SetRoomEnvActive(true);
                // Rebuild Nav Mesh
                GameObject.Find("Default Room").GetComponent<NavMeshSurface>().BuildNavMesh();
                // Validation of waypoints and patrol points after rebuilt
                WaypointsValidation();
                rooms[roomNumberLocal].SetRoomObjectsActive(true);
                Debug.Log("Direction " + direction + ": Room" + (roomNumberLocal - pace) + " --> Room" + roomNumberLocal);
                // set properties to update localplayer's gameobject in other devices
                // Hashtable userProperty = new Hashtable();
                // userProperty[playerLocationKey] = roomNumberLocal;
                // PhotonNetwork.LocalPlayer.SetCustomProperties(userProperty);
                // DisplayPlayersInCurrentRoom();
                // reset the entrance's animation
                AnimationState state = ani["DoorOpen"];
                state.time = 0;
                ani.Sample();
                state.enabled = false;

                // reset wait count after entered a new room
                Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
                roomProperties["Down"] = 0;
                roomProperties["Left"] = 0;
                roomProperties["Right"] = 0;
                roomProperties["Up"] = 0;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

                ItemTrigger.entranceFullyOpenEvent.RemoveAllListeners();
                entranceWaiting = false;
                entranceAvailable = true;
                localPlayerInstance.SendMessage("SetLockAllInput", false);
            });
            // sort all gameobjects related to roomNumber in others' client
            if(!PhotonNetwork.IsMasterClient){
                foreach (Room room in rooms)
                {
                    if(!room.setup) room.GetReady();
                }
            }
        }

        // Update is called once per frame
        void Update()
        { 
            if (Input.GetButtonDown("A")){
                ActionTriggerEvent.Invoke();
            }
        }
    }
}
