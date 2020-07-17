using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Invector;

using Photon.Pun;

public class RoomManager : SingletonMonoBehaviour<RoomManager> {
    public EnemiesConfig DungeonEnemiesConfig { get; private set; }

    public LoadObject[] EnemiesSpawns { get; private set; }
    public LoadObject[] BossRoomSpawns { get; private set; }
    public Range2DRect ObstaclesInnerBound { get; private set; }
    public Range2DRect ObstaclesOuterBound { get; private set; }

    public int MapSize { get; private set; }
    public bool entranceAvailable { get; private set; } = true;

    private bool firstBuild = true;
    private Dictionary<Direction, GameObject> EntranceDict;
    private Dictionary<RoomType, GameObject> RoomExtraDict;
    private GameObject defaultRoom;
    private GameObject WaypointArea;
    private List<Room> rooms = new List<Room>();
    private int PlayerLocation = 0;
    private int EmptyRoomNumber;

    public RoomType GetRoomTypeByIndex(int idx) {
        return rooms[idx].roomType;
    }

    public int GetPlayerLocation() {
        return PlayerLocation;
    }

    protected override void Init() {
        MapSize = (int)RoomPropManager.Instance.GetProp(RoomPropType.MapSize);
        DungeonEnemiesConfig = Resources.Load("Configs/DungeonEnemiesConfig") as EnemiesConfig;

        LoadPositionConfig enemiesSpawnConfig = Resources.Load("Configs/EnemiesSpawnConfig") as LoadPositionConfig;
        EnemiesSpawns = new LoadObject[enemiesSpawnConfig.loads.Count];
        enemiesSpawnConfig.loads.CopyTo(EnemiesSpawns);

        LoadPositionConfig bossRoomSpawnConfig = Resources.Load("Configs/BossRoomSpawnConfig") as LoadPositionConfig;
        BossRoomSpawns = new LoadObject[bossRoomSpawnConfig.loads.Count];
        bossRoomSpawnConfig.loads.CopyTo(BossRoomSpawns);

        LoadPositionConfig obstaclesRangeConfig = Resources.Load("Configs/ObstaclesRangeConfig") as LoadPositionConfig;
        ObstaclesInnerBound = obstaclesRangeConfig.innerBound;
        ObstaclesOuterBound = obstaclesRangeConfig.outerBound;

        EntranceDict = new Dictionary<Direction, GameObject>() {
            { Direction.Down, GameObject.Find("Entrance(Down)") },
            { Direction.Left, GameObject.Find("Entrance(Left)") },
            { Direction.Right, GameObject.Find("Entrance(Right)") },
            { Direction.Up, GameObject.Find("Entrance(Up)") },
        };

        RoomExtraDict = new Dictionary<RoomType, GameObject>() {
            {RoomType.EmptyRoom, null },
            {RoomType.StartingRoom, Instantiate(Resources.Load<GameObject>("Prefabs/RoomExtras/Starting_Room_Extra")) },
            {RoomType.BattleRoom, Instantiate(Resources.Load<GameObject>("Prefabs/RoomExtras/Battle_Room_Extra")) },
            {RoomType.RewardRoom, Instantiate(Resources.Load<GameObject>("Prefabs/RoomExtras/Reward_Room_Extra")) },
            {RoomType.BossRoom, Instantiate(Resources.Load<GameObject>("Prefabs/RoomExtras/Boss_Room_Extra")) },
        };
        RoomExtraDict[RoomType.StartingRoom].SetActive(false);
        RoomExtraDict[RoomType.BattleRoom].SetActive(false);
        RoomExtraDict[RoomType.RewardRoom].SetActive(false);
        RoomExtraDict[RoomType.BossRoom].SetActive(false);

        MessageCenter.Instance.AddObserver(NetEventCode.MoveRoom, MoveRoomLogic);
        MessageCenter.Instance.AddEventListener(GLEventCode.StartRoomTransition, OnRoomTransitionStart);
        MessageCenter.Instance.AddEventListener(GLEventCode.EndRoomTransition, OnRoomTransitionEnd);
    }

    public void SetUpRoomMap() {
        object[] roomtypes = new object[MapSize * MapSize];
        int index = 0;
        roomtypes[index++] = RoomType.StartingRoom;
        roomtypes[index++] = RoomType.EmptyRoom;
        while (index < MapSize * MapSize / 4 + 2) {
            roomtypes[index++] = RoomType.RewardRoom;
        }
        while (index != MapSize * MapSize - 1) {
            roomtypes[index++] = RoomType.BattleRoom;
        }
        roomtypes[index] = RoomType.BossRoom;
        roomtypes.Shuffle(1, MapSize * MapSize - 1);

        RoomPropManager.Instance.SetProp(RoomPropType.GridMap, roomtypes);
    }

    public void BuildAndCreateMap(object[] roomtypes) {
        if (firstBuild) {
            Room.roomsContainer = new GameObject("rooms");
            defaultRoom = GameObject.Find("Default Room");
            WaypointArea = GameObject.Find("WaypointArea");
            firstBuild = false;
        } else {
            HideRoom(PlayerLocation);
            foreach (Room room in rooms) {
                room.CleanUp();
            }
            rooms.Clear();
        }

        PlayerLocation = 0;
        // agents need valid NavMesh
        defaultRoom.GetComponent<NavMeshSurface>().BuildNavMesh();
        for (int roomIdx = 0; roomIdx < MapSize * MapSize; roomIdx++) {
            RoomType type = (RoomType)roomtypes[roomIdx];
            if (type == RoomType.EmptyRoom) {
                rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomIdx, RoomType.EmptyRoom));
                EmptyRoomNumber = roomIdx;
            } else {
                rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomIdx, type, RoomExtraDict[type]));
            }
        }
        LoadRoom(0);
    }

    public int GetPace(int roomIdx, Direction direction) {
        int pace;
        switch (direction) {
            // check if the room is on the edge
            case Direction.Down:
                pace = roomIdx / MapSize == 0 ? 0 : -MapSize;
                break;
            case Direction.Left:
                pace = roomIdx % MapSize == 0 ? 0 : -1;
                break;
            case Direction.Right:
                pace = roomIdx % MapSize == MapSize - 1 ? 0 : 1;
                break;
            case Direction.Up:
                pace = roomIdx / MapSize == MapSize - 1 ? 0 : MapSize;
                break;
            default:
                Debug.LogError("Wrong Direction!");
                pace = 0;
                break;
        }
        return pace;
    }

    // triggered on Room Icon Select
    public bool[] SiblingCheck(int roomNumber) {
        bool[] avail = new bool[] { false, false, false, false };
        avail[(int)Direction.Down] = roomNumber - MapSize == EmptyRoomNumber;
        avail[(int)Direction.Left] = roomNumber % MapSize != 0 && roomNumber - 1 == EmptyRoomNumber;
        avail[(int)Direction.Right] = roomNumber % MapSize != MapSize - 1 && roomNumber + 1 == EmptyRoomNumber;
        avail[(int)Direction.Up] = roomNumber + MapSize == EmptyRoomNumber;
        return avail;
    }

    public bool AvailableAhead(Direction dir) {
        int pace = GetPace(PlayerLocation, dir);
        return !(pace == 0 || rooms[PlayerLocation + pace].IsEmptyRoom());
    }

    public void AddEnemy(GameObject enemy, int roomIdx) {
        if (rooms.Count > roomIdx) {
            rooms[roomIdx].AddEnemy(enemy);
        } else {
            Debug.LogErrorFormat("RoomManager.AddEnemy(): room {0} index out of range {1}", roomIdx, rooms.Count);
        }
    }

    public void AddObstacle(GameObject obstacle, int roomIdx) {
        if(rooms.Count > roomIdx) {
            rooms[roomIdx].AddObstacle(obstacle);
        } else {
            Debug.LogErrorFormat("RoomManager.AddObstacle(): room {0} index out of range {1}", roomIdx, rooms.Count);
        }
    }

    /// <summary>
    /// some points in waypoint area might not be available because of the randomly generated obstacles
    /// so check every point in the area and reset their variables
    /// </summary>
    private void WaypointsValidation() {
        vPoint[] points = WaypointArea.GetComponentsInChildren<vPoint>();
        foreach (var point in points) {
            point.SetValidIfInNavMesh();
        }
    }

    public void GenerateAllSceneObjects() {
        foreach (Room room in rooms) {
            room.GenerateSceneObjects();
        }
    }

    public void EnterRoomInDirection(Direction direction) {
        int pace = GetPace(PlayerLocation, direction);
        HideRoom(PlayerLocation);
        // update currentLocation
        PlayerLocation += pace;
        LoadRoom(PlayerLocation);
        Debug.Log("Direction " + direction + ": Room" + (PlayerLocation - pace) + " --> Room" + PlayerLocation);
    }

    public void OnMasterBossDeath(GameObject boss) {
        // false -> 侵入者胜利
        MessageCenter.Instance.PostNetEvent2All(NetEventCode.GameClear, false);
    }

    // room can only be moved to the empty room's position
    // triggered on Move Button Down
    private void MoveRoomLogic(object data) {
        object[] content = (object[])data;
        int selectIdx = (int)content[0];
        Direction dir = (Direction)content[1];

        int moveTo = selectIdx + GetPace(selectIdx, dir);

        if (selectIdx == moveTo) {
            Debug.LogErrorFormat("Can't move grid{0} to grid{1}", selectIdx, moveTo);
        } else if (moveTo != EmptyRoomNumber) {
            Debug.LogErrorFormat("No empty room in direction {0}", dir);
        } else {
            Debug.LogFormat("Room at grid{0} is moving to grid{1}", selectIdx, moveTo);
        }

        Room T = rooms[moveTo];
        rooms[moveTo] = rooms[selectIdx];
        rooms[selectIdx] = T;
        // if players are currently in room selectIdx
        if (PlayerLocation == selectIdx) {
            PlayerLocation = moveTo;
        }
        // move from selectIdx to moveTo, now selectIdx is empty
        EmptyRoomNumber = selectIdx;
    }

    private void OnRoomTransitionStart(object data) {
        Direction direction = (Direction)data;
        entranceAvailable = false;
        if (EntranceDict.ContainsKey(direction)) {
            GameObject entrance = EntranceDict[direction];
            object[] _data = new object[] { entrance.GetInstanceID(), entrance.GetComponent<Entrance>().animationPlayed };
            MessageCenter.Instance.PostGLEvent(GLEventCode.PlayAnimation, _data);

            WaitTimeManager.CreateCoroutine(false, GameManager.Instance.timeConfig.EntranceAnimation, () => {
                MessageCenter.Instance.PostGLEvent(GLEventCode.EndRoomTransition, direction);
            });
        }
    }

    private void OnRoomTransitionEnd(object data) {
        Direction direction = (Direction)data;
        EnterRoomInDirection(direction);
        if (EntranceDict.ContainsKey(direction)) {
            GameObject ent = EntranceDict[direction];
            object[] _data = new object[] { ent.GetInstanceID(), ent.GetComponent<Entrance>().animationPlayed };
            MessageCenter.Instance.PostGLEvent(GLEventCode.ResetAnimation, _data);
        }
        entranceAvailable = true;
    }

    private void HideRoom(int roomIdx) {
        rooms[roomIdx].SetRoomEnvActive(false);
        rooms[roomIdx].SetRoomObjectsActive(false);
    }

    private void LoadRoom(int roomIdx) {
        rooms[roomIdx].SetRoomEnvActive(true);
        // Rebuild Nav Mesh
        defaultRoom.GetComponent<NavMeshSurface>().BuildNavMesh();
        WaypointsValidation();
        rooms[roomIdx].SetRoomObjectsActive(true);
    }
}