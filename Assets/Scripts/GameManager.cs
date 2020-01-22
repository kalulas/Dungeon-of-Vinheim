using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class IListExtensions {
    /// <summary>
    /// Shuffles the element order of the specified list. [start, end)
    /// </summary>
    public static void Shuffle<T>(this IList<T> ts, int start, int end) {
        // var count = ts.Count;
        // var last = count - 1;
        for (int i = start; i < end; ++i) {
            int r = UnityEngine.Random.Range(i, end);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}

// 单例脚本管理地图信息并进行关卡切换
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    /// <summary>
    /// positions stored for player's reposition when he enters a new room
    /// </summary>
    public static Vector3[] positions = new Vector3[5]{
        new Vector3(15.4f, 0.15f, 0f), // down 
        new Vector3(-1.3f, 0.15f, 16.66f), // left
        new Vector3(32.22f, 0.15f, 16.73f), // right
        new Vector3(15.36f, 0.15f, 33.23f), // up
        new Vector3(15.45f, 0.15f, 16.66f) //center
    };
    // the reference of player's transform component
    public Transform playerTransform;
    private int mapSize = 5;
    private int currentLocation;
    private int emptyRoomNumber;
    private List<Room> rooms = new List<Room>();
    // private List<Transform> positions = new List<Transform>(5);
    
    static GameManager(){
        GameObject gm = new GameObject("#GameManager#");
        DontDestroyOnLoad(gm);
        instance = gm.AddComponent<GameManager>();
    }

    public void ShowYourself(){
        Debug.Log("Game Manager exists.");
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("instance of 'Game Manager' generated.");
        // player starts from room 0 - starting room
        currentLocation = 0;
        // Load Starting Room Extra (only once)
        GameObject roomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/Starting Room Extra"));
        rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(RoomType.StartingRoom, roomExtra));

        // Load Empty Room
        rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(RoomType.EmptyRoom));
        
        // Load Reward Room Prefab once and set active false to hide it
        roomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/Reward Room Extra"));
        roomExtra.SetActive(false);
        // Add Reward Rooms with the prefab but different items
        for (int index = 0; index <  mapSize * mapSize / 4; index++)
        {
            rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(RoomType.RewardRoom, roomExtra));
        }
        
        // Load Battle Room Prefab and add Rooms
        roomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/Battle Room Extra"));
        roomExtra.SetActive(false);
        // Add Battle Rooms with the prefab but different items
        while(rooms.Count != mapSize * mapSize - 1)
        {
            rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(RoomType.BattleRoom, roomExtra));
        }
        
        // Load Boss Room Prefab and add Room
        roomExtra = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/Boss Room Extra"));
        roomExtra.SetActive(false);
        rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(RoomType.BossRoom, roomExtra));
        // Randomizing the mapSize * mapSize dungeon
        rooms.Shuffle(1, mapSize * mapSize - 1);
        
        // initialize empty room number
        for (int i = 0; i < rooms.Count; i++)
        {
            // if (rooms[i].Empty()) { emptyRoomNumber = i; break; }
            string roomtype = rooms[i].GetRoomType().ToString();
            Debug.Log("Room" + i + " : " + roomtype);
        }

        // justify player position
        playerTransform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        playerTransform.position = positions[4];

        // Add Environment(like entrances) Trigger Listener
        ItemTrigger.triggerEventStay.AddListener(delegate (Collider collider){
            // TODO: use GUI to show information and ask for permission
            if(Input.GetButtonDown("Confirm")){
                if(ItemTrigger.triggerEventStay.type == TriggerEventType.Entrance){
                    HandleEntranceTriggerEvent(ItemTrigger.triggerEventStay.direction);
                }
            }
        });

    }

    // triggered on Room Icon Select
    public void SiblingCheck(int roomNumber){
        // can be only one direction available
        if(roomNumber - mapSize == emptyRoomNumber){
            // Enable Down Button
        }
        else if(roomNumber % mapSize != 0 && roomNumber - 1 == emptyRoomNumber){
            // Enable Left Button
        }
        else if(roomNumber % mapSize != mapSize-1 && roomNumber + 1 == emptyRoomNumber){
            // Enable Right Button
        }
        else if(roomNumber + mapSize == emptyRoomNumber){
            // Enable Up Button
        }
    }

    // room can only be moved to the empty room's position
    // triggered on Move Button Down
    public void MoveRoom(int roomNumber){
        Room T = rooms[emptyRoomNumber];
        rooms[emptyRoomNumber] = rooms[roomNumber];
        rooms[roomNumber] = T;
        // update empty room number
        emptyRoomNumber = roomNumber;
    }

    /// <summary>
    /// handle triggerevent when the player is trying to enter the next room
    /// </summary>
    private void HandleEntranceTriggerEvent(Direction direction){
        // EnterDirection direction = ItemTrigger.triggerEventStay.direction;
        int pace;
        switch (direction)
        {
            // check if the room is on the edge
            case Direction.Down: pace= currentLocation/mapSize ==0?0:-mapSize;break;
            case Direction.Left: pace= currentLocation%mapSize ==0?0:-1;break;
            case Direction.Right: pace= currentLocation%mapSize ==mapSize-1?0:1;break;
            case Direction.Up: pace= currentLocation/mapSize ==mapSize-1?0:mapSize;break;
            default: pace = 0;break;
        }
        if(pace == 0 || rooms[currentLocation+pace].Empty()) Debug.Log("BLOCKED");
        else{
            rooms[currentLocation].SetRoomActive(false);
            // update currentLocation
            currentLocation += pace;
            // change player's position (Tranform component)
            // TODO: use animation to cover up here
            switch (direction)
            {
                // check if the room is on the edge
                case Direction.Down: playerTransform.position = positions[3];break;
                case Direction.Left: playerTransform.position = positions[2];break;
                case Direction.Right: playerTransform.position = positions[1];break;
                case Direction.Up: playerTransform.position = positions[0];break;
                default:break;
            }
            rooms[currentLocation].SetRoomActive(true);
            Debug.Log("Direction " + direction + ": Room" + (currentLocation-pace) + " --> Room" + currentLocation);
        }

    }

    // Update is called once per frame
    // void Update()
    // {
        
    // }
}
