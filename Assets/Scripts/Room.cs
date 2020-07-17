using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Invector.vCharacterController.AI;

using Photon.Pun;
using ExitGames.Client.Photon;

public enum RoomType {
    EmptyRoom,
    StartingRoom,
    BattleRoom,
    RewardRoom,
    BossRoom
}

// Now GameManager can create instance of ScriptableObject Room

[CreateAssetMenu]
public class Room : ScriptableObject {
    // private int roomIndex;
    public static GameObject roomsContainer;
    [SerializeField]
    private int roomNumber;
    public RoomType roomType { get; private set; }

    // private RoomType roomType;
    [SerializeField]
    private GameObject roomExtra;
    [SerializeField]
    private GameObject roomObjectsContainer, enemiesContainer, obstaclesContainer, itemsContainer;
    [SerializeField]
    // the upper limit of the number of enemies
    private const int enemyCountLimit = 9;
    [SerializeField]
    private List<GameObject> enemyList = new List<GameObject>();
    [SerializeField]
    private List<GameObject> obstacleList = new List<GameObject>();
    [SerializeField]
    private List<GameObject> itemList = new List<GameObject>();

    private Vector3 GetRandomPositionInRange(Vector3[] inner, Vector3[] outer) {
        float x = Random.Range(outer[0].x, outer[2].x);
        float z = 0f;
        if (x < inner[0].x || x > inner[2].x) {
            z = Random.Range(outer[0].z, outer[2].z);
        } else {
            float z1 = Random.Range(outer[0].z, inner[0].z);
            float z2 = Random.Range(inner[2].z, outer[2].z);
            z = Random.Range(0f, 1f) < 0.5 ? z1 : z2;
        }
        return new Vector3(x, 0f, z);
    }

    private void GenerateBossRoomSceneObjects() {
        LoadObject[] enemiesSpawns = RoomManager.Instance.BossRoomSpawns;
        object[] data = { InstantiateType.NewEnemy, roomNumber };
        int bossCount = RoomManager.Instance.DungeonEnemiesConfig.bossPathList.Count;
        if (bossCount == 0) {
            Debug.LogError("Boss configs not found in DungeonEnemiesConfig.bossPathList");
        }
        string chosenBoss = RoomManager.Instance.DungeonEnemiesConfig.bossPathList[Random.Range(0, bossCount - 1)];
        GameObject boss = PhotonNetwork.InstantiateSceneObject(chosenBoss, enemiesSpawns[0].position, Quaternion.Euler(enemiesSpawns[0].rotation), 0, data);
        // Master Client Death Event Monitor
        boss.GetComponent<v_AIController>()?.onDead.AddListener(RoomManager.Instance.OnMasterBossDeath);
    }

    private void GenerateBattleRoomSceneObjects() {
        LoadObject[] enemiesSpawns = RoomManager.Instance.EnemiesSpawns;
        Range2DRect InnerBound = RoomManager.Instance.ObstaclesInnerBound;
        Range2DRect OuterBound = RoomManager.Instance.ObstaclesOuterBound;

        GameObject[] obstacles = Resources.LoadAll<GameObject>("Prefabs/Environments/Rocks");
        obstacles.Shuffle(0, obstacles.Length);

        int enemyCount = Random.Range(1, enemyCountLimit);
        int obstacleCount = Random.Range(obstacles.Length / 2, obstacles.Length);

        for (int i = 0; i < enemyCount; i++) {
            object[] data = { InstantiateType.NewEnemy, roomNumber };

            int enemyType = RoomManager.Instance.DungeonEnemiesConfig.normalPathList.Count;
            if (enemyType == 0) {
                Debug.LogError("Can't find any enemies prefab in DungeonEnemiesConfig.normalPathList");
            }

            // enemies are randomly generated.
            string chosenEnemy = RoomManager.Instance.DungeonEnemiesConfig.normalPathList[Random.Range(0, enemyType - 1)];
            GameObject enemy = PhotonNetwork.InstantiateSceneObject(chosenEnemy, enemiesSpawns[i].position, Quaternion.Euler(enemiesSpawns[i].rotation), 0, data);
            // enemy.GetComponent<v_AIMotor>().pathArea = pathArea;
        }

        for(int i = 0; i < obstacleCount; i++) {
            object[] data = { InstantiateType.NewObstacle, roomNumber };
            string chosenObstacle = "Prefabs/Environments/Rocks/" + obstacles[i].name;
            Vector3 gPosition = GetRandomPositionInRange(InnerBound.GetBorder(), OuterBound.GetBorder());
            GameObject obstacle = PhotonNetwork.InstantiateSceneObject(chosenObstacle, gPosition, Quaternion.identity, 0, data);
        }

    }

    public void GenerateSceneObjects() {
        switch (roomType) {
            case RoomType.EmptyRoom:
                break;
            case RoomType.StartingRoom:
                break;
            case RoomType.BattleRoom:
                GenerateBattleRoomSceneObjects();
                break;
            case RoomType.RewardRoom:
                break;
            case RoomType.BossRoom:
                GenerateBossRoomSceneObjects();
                break;
            default:
                break;
        }
    }

    public Room SetUp(int number, RoomType type, GameObject extra = null) {
        roomType = type;
        roomExtra = extra;
        roomNumber = number;
        // 准备好场景对象的容器
        roomObjectsContainer = new GameObject("room" + roomNumber + roomType);
        roomObjectsContainer.transform.SetParent(roomsContainer.transform);
        //GetReady();
        enemiesContainer = new GameObject("enemies");
        enemiesContainer.transform.SetParent(roomObjectsContainer.transform);
        obstaclesContainer = new GameObject("obstacles");
        obstaclesContainer.transform.SetParent(roomObjectsContainer.transform);
        return this;
    }

    public void CleanUp() {
        // GameObjects associated with photonViews are destroyed by MasterClient
        if (PhotonNetwork.IsMasterClient) {
            foreach (GameObject enemy in enemyList) {
                if (enemy != null) {
                    PhotonNetwork.Destroy(enemy);
                }
            }
            foreach (GameObject obstacle in obstacleList) {
                if (obstacle != null) {
                    PhotonNetwork.Destroy(obstacle);
                }
            }
        }

        foreach (GameObject item in itemList) {
            if (item != null) {
                Destroy(item);
            }
        }
        enemyList.Clear();
        obstacleList.Clear();
        itemList.Clear();

        // 会连带删掉场景同步对象，所以先保留旧容器
        //if(roomObjectsContainer != null) {
        //    Destroy(roomObjectsContainer);
        //}
    }

    public void AddEnemy(GameObject enemy) {
        if(enemiesContainer != null) {
            enemy.transform.SetParent(enemiesContainer.transform);
            enemyList.Add(enemy);
            if(roomNumber == RoomManager.Instance.GetPlayerLocation()) {
                enemy.SetActive(true);
            }
        } else {
            Debug.LogErrorFormat("enemiesContainer of room{0} not set up!", roomNumber);
        }
    }

    public void AddObstacle(GameObject obstacle) {
        if (obstaclesContainer != null) {
            obstacle.transform.SetParent(obstaclesContainer.transform);
            obstacleList.Add(obstacle);
            if (roomNumber == RoomManager.Instance.GetPlayerLocation()) {
                obstacle.SetActive(true);
            }
        } else {
            Debug.LogErrorFormat("obstaclesContainer of room{0} not set up!", roomNumber);
        }
    }

    public bool IsEmptyRoom() {
        return roomType == RoomType.EmptyRoom;
    }

    public void SetRoomObjectsActive(bool value) {
        if (enemyList != null) {
            foreach (GameObject enemy in enemyList) {
                if(enemy != null) {
                    enemy.SetActive(value);
                }
            }
        }
        if (itemList != null) {
            foreach (GameObject item in itemList) {
                if(item!= null) {
                    item.SetActive(value);
                }
            }
        }
    }

    public void SetRoomEnvActive(bool value) {
        // rotate the content of the boss room, so player will be in the front of the boss
        // iN fact the player can only enter from direction "down" or "left"
        //if (value) {
        //    if (roomType == RoomType.BossRoom) {
        //        float angleY = GameManager.localPlayerInstance.GetComponent<Transform>().position == GameManager.positions[0] ? 180 : 270;
        //        roomExtra.transform.rotation = Quaternion.Euler(0, angleY, 0);
        //    }
        //}
        if (obstacleList != null) {
            foreach (var obstacle in obstacleList) obstacle.SetActive(value);
        }
        roomExtra.SetActive(value);
    }
}
