using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// 单例脚本管理地图信息并进行关卡切换
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private int mapSize = 3;
    private int currentLocation;
    private List<Room> rooms = new List<Room>();
    /// <summary>
    /// positions for player's reposition when he enters a new room
    /// </summary>
    private List<Transform> positions = new List<Transform>(5);

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
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Starting Room Extra.prefab");
        GameObject roomExtra = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        rooms.Add(new Room(RoomType.StartingRoom, roomExtra));

        // Load Empty Room
        rooms.Add(new Room(RoomType.EmptyRoom));
        
        // Load Reward Room Prefab once and set active false to hide it
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Reward Room Extra.prefab");
        roomExtra = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        roomExtra.SetActive(false);
        // Add Reward Rooms with the prefab but different items
        for (int index = 0; index < mapSize - 1; index++)
        {
            // TODO: randomly generate items here
            // List<GameObject> itemList = new List<GameObject>();
            rooms.Add(new Room(RoomType.RewardRoom, roomExtra));
        }
        
        // Load Battle Room Prefab and add Rooms
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Battle Room Extra.prefab");
        roomExtra = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        roomExtra.SetActive(false);
        // Add Battle Rooms with the prefab but different items
        while(rooms.Count != mapSize * mapSize - 1)
        {
            // TODO: randomly generate enemies here
            // List<GameObject> enemyList = new List<GameObject>();
            rooms.Add(new Room(RoomType.BattleRoom, roomExtra));
        }
        
        // Load Boss Room Prefab and add Room
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boss Room Extra.prefab");
        roomExtra = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        roomExtra.SetActive(false);
        rooms.Add(new Room(RoomType.BossRoom, roomExtra));

        // records and justify player position
        // TODO: might be another way
        positions.Add(GameObject.Find("FromDirectionDown").GetComponent<Transform>());
        positions.Add(GameObject.Find("FromDirectionLeft").GetComponent<Transform>());
        positions.Add(GameObject.Find("FromDirectionRight").GetComponent<Transform>());
        positions.Add(GameObject.Find("FromDirectionUp").GetComponent<Transform>());
        positions.Add(GameObject.Find("FromStart").GetComponent<Transform>());
        Transform transform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        transform = positions[4];
        // transform.rotation = positions[4].rotation;

        // Add Gate Trigger Listener
        TriggerListener.triggerEventEnter.AddListener(delegate (Collider collider){
            if(TriggerListener.triggerEventEnter.type == TriggerEventType.Entrance){
                // TODO: use GUI to show information and ask for permission
                HandleEntranceTriggerEvent(collider);
            }
        });
    }

    /// <summary>
    /// handle triggerevent when the player is trying to enter the next room
    /// </summary>
    private void HandleEntranceTriggerEvent(Collider collider){
        EnterDirection direction = TriggerListener.triggerEventEnter.direction;
        int pace;
        switch (direction)
        {
            // check if the room is on the edge
            case EnterDirection.Down: pace= currentLocation/mapSize ==0?0:-3;break;
            case EnterDirection.Left: pace= currentLocation%mapSize ==0?0:-1;break;
            case EnterDirection.Right: pace= currentLocation%mapSize ==2?0:1;break;
            case EnterDirection.Up: pace= currentLocation/mapSize ==mapSize-1?0:3;break;
            default: pace = 0;break;
        }
        if(pace == 0 || rooms[currentLocation+pace].Empty()) Debug.Log("BLOCKED");
        else{
            rooms[currentLocation].SetRoomActive(false);
            // update currentLocation
            currentLocation += pace;
            // change player's position (Tranform component)
            // TODO: use animation to cover up here
            Transform transform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
            switch (pace)
            {
                case -3:transform.position = positions[3].position;break;
                case -1:transform.position = positions[2].position;break;
                case 1:transform.position = positions[1].position;break;
                case 3:transform.position = positions[0].position;break;
                default:break;
            }
            rooms[currentLocation].SetRoomActive(true);
            Debug.Log("entering room in direction " + direction + ", now you are at Room" + currentLocation);
        }

    }

    // Update is called once per frame
    // void Update()
    // {
        
    // }
}
