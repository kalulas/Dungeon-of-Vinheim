using System.Collections;
using System.Collections.Generic;
using Invector;
using UnityEngine;
using UnityEngine.AI;
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
namespace DungeonOfVinheim
{
    // 单例脚本管理地图信息并进行关卡切换
    public class GameManager : MonoBehaviourPunCallbacks
    {
        public static GameManager instance;
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
        // the reference of player's transform component
        public GameObject player { get; private set; }
        // public Transform playerTransform{ get; private set; }

        // patrol area for enemies
        public GameObject WaypointArea;
        public GameObject enemiesSpawnPointsContainer;
        // transform data for enemies' spawning
        public List<Transform> enemiesSpawnPoints = new List<Transform>();
        // transform data for obstacles' spawning
        public List<Transform> obstaclesSpawnPoints = new List<Transform>();

        public int mapSize { get; private set; } = 5;
        public int emptyRoomNumber { get; private set; }
        public int currentLocation { get; private set; }
        private bool entranceAvailable = true;
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

        public void LeaveRoom(){
            PhotonNetwork.LeaveRoom();
        }

        // Start is called before the first frame update
        void Awake()
        {
#if !UNITY_EDITOR
        Debug.unityLogger.logEnabled = false;
#endif
            // now game manager is attached to gameobject 'Game'
            instance = this;
            Debug.Log("instance of 'Game Manager' generated.");
            currentLocation = 0;
            int roomNumber = 0;
            Room.roomsContainer = new GameObject("rooms");
            defaultRoom = GameObject.Find("Default Room");

            // justify player position
            player = GameObject.FindGameObjectWithTag("Player");
            player.GetComponent<Transform>().position = positions[4];

            // Load Starting Room Extra (only once)
            GameObject roomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/RoomExtras/Starting_Room_Extra"));
            rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.StartingRoom, roomExtra));
            // Build Default Nav Mesh
            defaultRoom.GetComponent<NavMeshSurface>().BuildNavMesh();

            // Load Empty Room
            rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.EmptyRoom));

            // Load Reward Room Prefab once and set active false to hide it
            roomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/RoomExtras/Reward_Room_Extra"));
            roomExtra.SetActive(false);
            // Add Reward Rooms with the prefab but different items
            for (int index = 0; index < mapSize * mapSize / 4; index++)
            {
                rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.RewardRoom, roomExtra));
            }

            // Load Battle Room Prefab and add Rooms
            enemiesSpawnPoints.Shuffle(0, enemiesSpawnPoints.Count);
            roomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/RoomExtras/Battle_Room_Extra"));

            enemiesSpawnPointsContainer.GetComponentsInChildren<Transform>(true, enemiesSpawnPoints);
            GameObject randomObstacles = GameObject.Find("Random Obstacles");
            randomObstacles.GetComponentsInChildren<Transform>(true, obstaclesSpawnPoints);
            // Add Battle Rooms with the prefab but different items
            while (rooms.Count != mapSize * mapSize - 1)
            {
                rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.BattleRoom, roomExtra));
            }
            roomExtra.SetActive(false);

            // Load Boss Room Prefab and add Room
            roomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/RoomExtras/Boss_Room_Extra"));
            roomExtra.SetActive(false);
            rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.BossRoom, roomExtra));
            // Randomizing the mapSize * mapSize dungeon
            rooms.Shuffle(1, mapSize * mapSize - 1);

            // initialize empty room number
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].Empty()) { emptyRoomNumber = i; }
                string roomtype = rooms[i].roomType.ToString();
                Debug.Log("Room" + i + " : " + roomtype);
            }

            // Add Environment(like entrances) Trigger Listener
            // ⬇ also available
            // triggerEventStay.AddListener((Collider collider, GameObject item)=>{
            ItemTrigger.triggerEventStay.AddListener(delegate (Collider collider, GameObject item)
            {
                if (Input.GetButtonDown("A"))
                {
                    if (ItemTrigger.triggerEventStay.type == TriggerEventType.Entrance)
                    {
                        if (entranceAvailable) HandleEntranceEvent(item, ItemTrigger.triggerEventStay.direction);
                        // else can be locked by the host
                    }
                }
            });

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

        /// <summary>
        /// handle triggerevent when the player is trying to enter the next room
        /// </summary>
        private void HandleEntranceEvent(GameObject entrance, Direction direction)
        {
            int pace;
            switch (direction)
            {
                // check if the room is on the edge
                case Direction.Down:
                    pace = currentLocation / mapSize == 0 ? 0 : -mapSize;
                    break;
                case Direction.Left:
                    pace = currentLocation % mapSize == 0 ? 0 : -1;
                    break;
                case Direction.Right:
                    pace = currentLocation % mapSize == mapSize - 1 ? 0 : 1;
                    break;
                case Direction.Up:
                    pace = currentLocation / mapSize == mapSize - 1 ? 0 : mapSize;
                    break;
                default:
                    pace = 0;
                    break;
            }
            if (pace == 0 || rooms[currentLocation + pace].Empty())
            {
                Debug.Log("BLOCKED");
                // notify player that no room ahead, also no need for prefix "press e to"
                UIManager.setActionTextContentEvent.Invoke("BLOCKED", false);
            }
            else
            {
                // prevent from player's pressing too many times
                entranceAvailable = false;
                player.SendMessage("SetLockAllInput", true);
                // NOTE: no TRIGGER EXIT event now so manually set action text false
                UIManager.setActionTextActiveEvent.Invoke(false);
                // TODO: play smoke animation & player's open door animation maybe you need to reposition the player
                Animation ani = entrance.GetComponent<Animation>();
                ani.Play("DoorOpen");
                // wait until the entrance is fully open
                ItemTrigger.entranceFullyOpenEvent.AddListener(delegate ()
                {
                    rooms[currentLocation].SetRoomEnvActive(false);
                    rooms[currentLocation].SetRoomObjectsActive(false);
                    // update currentLocation
                    currentLocation += pace;
                    // change player's position (Tranform component)
                    switch (direction)
                    {
                        // check if the room is on the edge
                        case Direction.Down:
                            player.GetComponent<Transform>().position = positions[3];
                            break;
                        case Direction.Left:
                            player.GetComponent<Transform>().position = positions[2];
                            break;
                        case Direction.Right:
                            player.GetComponent<Transform>().position = positions[1];
                            break;
                        case Direction.Up:
                            player.GetComponent<Transform>().position = positions[0];
                            break;
                        default:
                            break;
                    }
                    rooms[currentLocation].SetRoomEnvActive(true);
                    // Rebuild Nav Mesh
                    GameObject.Find("Default Room").GetComponent<NavMeshSurface>().BuildNavMesh();
                    // Validation of waypoints and patrol points after rebuilt
                    WaypointsValidation();
                    rooms[currentLocation].SetRoomObjectsActive(true);
                    Debug.Log("Direction " + direction + ": Room" + (currentLocation - pace) + " --> Room" + currentLocation);

                    // reset the entrance's animation
                    AnimationState state = ani["DoorOpen"];
                    state.time = 0;
                    ani.Sample();
                    state.enabled = false;

                    ItemTrigger.entranceFullyOpenEvent.RemoveAllListeners();
                    player.SendMessage("SetLockAllInput", false);
                    entranceAvailable = true;
                });
            }
        }

        public void UnityEventAction()
        {
            Debug.Log("UnityEventTest!");
        }

        // Update is called once per frame
        // void Update()
        // { 
        // }
    }
}
