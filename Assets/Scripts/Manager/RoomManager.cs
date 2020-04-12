using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Invector;

using Photon.Pun;

public class RoomManager : SingletonMonoBehaviour<RoomManager> {
    public LoadObject[] EnemiesSpawns { get; private set; }
    public Range2DRect ObstaclesInnerBound { get; private set; }
    public Range2DRect ObstaclesOuterBound { get; private set; }

    public int MapSize { get; private set; } = 5;
    public bool entranceAvailable { get; private set; } = true;
    private Dictionary<Direction, GameObject> EntrancesDic;
    private GameObject defaultRoom;
    private GameObject WaypointArea;
    private List<Room> rooms = new List<Room>();
    private int PlayerLocation = 0;
    private int EmptyRoomNumber;

    public RoomType GetRoomTypeByIndex(int idx) {
        return rooms[idx].roomType;
    }

    protected override void Init() {
        LoadPositionConfig enemiesSpawnConfig = Resources.Load("Configs/EnemiesSpawnConfig") as LoadPositionConfig;
        EnemiesSpawns = new LoadObject[enemiesSpawnConfig.loads.Count];
        enemiesSpawnConfig.loads.CopyTo(EnemiesSpawns);
        LoadPositionConfig obstaclesRangeConfig = Resources.Load("Configs/ObstaclesRangeConfig") as LoadPositionConfig;
        ObstaclesInnerBound = obstaclesRangeConfig.innerBound;
        ObstaclesOuterBound = obstaclesRangeConfig.outerBound;

        EntrancesDic = new Dictionary<Direction, GameObject>() {
            { Direction.Down, GameObject.Find("EntranceDown") },
            { Direction.Left, GameObject.Find("EntranceLeft") },
            { Direction.Right, GameObject.Find("EntranceRight") },
            { Direction.Up, GameObject.Find("EntranceUp") },
        };

        //moveRoomEvent.AddListener(MoveRoomLogic);
        MessageCenter.Instance.AddObserver(NetEventCode.MoveRoom, MoveRoomLogic);
        MessageCenter.Instance.AddEventListener(GLEventCode.StartRoomTransition, OnRoomTransitionStart);
        MessageCenter.Instance.AddEventListener(GLEventCode.EndRoomTransition, OnRoomTransitionEnd);
    }

    public object[] SetUpRoomMap() {
        object[] roomtypes = new object[MapSize * MapSize];
        int index = 0;
        roomtypes[index++] = RoomType.StartingRoom;
        roomtypes[index++] = RoomType.EmptyRoom;
        for (; index < MapSize * MapSize / 4 + 2;) {
            roomtypes[index++] = RoomType.RewardRoom;
        }
        while (index != MapSize * MapSize - 1) {
            roomtypes[index++] = RoomType.BattleRoom;
        }
        roomtypes[index] = RoomType.BossRoom;
        roomtypes.Shuffle(1, MapSize * MapSize - 1);
        return roomtypes;
    }

    public void BuildAndCreateMap(object[] roomtypes) {
        Room.roomsContainer = new GameObject("rooms");
        defaultRoom = GameObject.Find("Default Room");
        WaypointArea = GameObject.Find("WaypointArea");
        defaultRoom.GetComponent<NavMeshSurface>().BuildNavMesh();

        int roomNumber = 0;
        GameObject startingRoomExtra = Instantiate(Resources.Load<GameObject>("Prefabs/RoomExtras/Starting_Room_Extra"));
        GameObject rewardRoomExtra = Instantiate(Resources.Load<GameObject>("Prefabs/RoomExtras/Reward_Room_Extra"));
        GameObject battleRoomExtra = Instantiate(Resources.Load<GameObject>("Prefabs/RoomExtras/Battle_Room_Extra"));
        GameObject bossRoomExtra = Instantiate(Resources.Load<GameObject>("Prefabs/RoomExtras/Boss_Room_Extra"));
        for (int i = 0; i < MapSize * MapSize; i++) {
            switch ((RoomType)roomtypes[i]) {
                case RoomType.EmptyRoom:
                    rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.EmptyRoom));
                    EmptyRoomNumber = i;
                    break;
                case RoomType.StartingRoom:
                    rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.StartingRoom, startingRoomExtra));
                    break;
                case RoomType.BattleRoom:
                    rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.BattleRoom, battleRoomExtra));
                    break;
                case RoomType.RewardRoom:
                    rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.RewardRoom, rewardRoomExtra));
                    break;
                case RoomType.BossRoom:
                    rooms.Add(ScriptableObject.CreateInstance<Room>().SetUp(roomNumber++, RoomType.BossRoom, bossRoomExtra));
                    break;
                default: break;
            }
        }
        battleRoomExtra.SetActive(false);
        rewardRoomExtra.SetActive(false);
        bossRoomExtra.SetActive(false);
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
        return !(pace == 0 || rooms[PlayerLocation + pace].Empty());
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

    public void LoadRoomAtDirection(Direction direction) {
        // TODO: FIX THIS
        // sort all gameobjects related to roomNumber in others' client
        if (!PhotonNetwork.IsMasterClient) {
            foreach (Room room in rooms) {
                if (!room.setup) {
                    room.GetReady();
                }
            }
        }

        int pace = GetPace(PlayerLocation, direction);
        rooms[PlayerLocation].SetRoomEnvActive(false);
        rooms[PlayerLocation].SetRoomObjectsActive(false);
        // update currentLocation
        PlayerLocation += pace;
        rooms[PlayerLocation].SetRoomEnvActive(true);
        // Rebuild Nav Mesh
        defaultRoom.GetComponent<NavMeshSurface>().BuildNavMesh();
        WaypointsValidation();

        rooms[PlayerLocation].SetRoomObjectsActive(true);
        Debug.Log("Direction " + direction + ": Room" + (PlayerLocation - pace) + " --> Room" + PlayerLocation);
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
        if (EntrancesDic.ContainsKey(direction)) {
            GameObject entrance = EntrancesDic[direction];
            object[] _data = new object[] { entrance.GetInstanceID(), entrance.GetComponent<Entrance>().animationPlayed };
            MessageCenter.Instance.PostGLEvent(GLEventCode.PlayAnimation, _data);

            WaitTimeManager.CreateCoroutine(false, GameManager.Instance.timeConfig.EntranceAnimation, () => {
                MessageCenter.Instance.PostGLEvent(GLEventCode.EndRoomTransition, direction);
            });
        }
    }

    private void OnRoomTransitionEnd(object data) {
        Direction direction = (Direction)data;
        LoadRoomAtDirection(direction);
        if (EntrancesDic.ContainsKey(direction)) {
            GameObject ent = EntrancesDic[direction];
            object[] _data = new object[] { ent.GetInstanceID(), ent.GetComponent<Entrance>().animationPlayed };
            MessageCenter.Instance.PostGLEvent(GLEventCode.ResetAnimation, _data);
        }
        entranceAvailable = true;
    }
}