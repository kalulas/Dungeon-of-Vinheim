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

    public bool setup { get; private set; } = false;
    // private RoomType roomType;
    [SerializeField]
    private GameObject roomExtra;
    [SerializeField]
    private GameObject roomExtraObjectsContainer;
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

    public Room SetUp(int number, RoomType type, GameObject extra = null) {
        roomType = type;
        roomExtra = extra;
        roomNumber = number;
        roomExtraObjectsContainer = new GameObject("room" + roomNumber + roomType);
        roomExtraObjectsContainer.GetComponent<Transform>().SetParent(roomsContainer.GetComponent<Transform>());
        if (PhotonNetwork.IsMasterClient) {
            // TODO: create enemies & items here
            switch (type) {
                case RoomType.BattleRoom: SetUpBattleRoom(); break;
                default: break;
            }
            setup = true;
        }
        itemList = null;
        return this;
    }

    public void GetReady() {
        switch (roomType) {
            case RoomType.BattleRoom: SetUpBattleRoom(); break;
            default: break;
        }
        setup = true;
    }

    private void SetUpBattleRoom() {
        LoadObject[] enemiesSpawns = RoomManager.Instance.EnemiesSpawns;
        Range2DRect InnerBound = RoomManager.Instance.ObstaclesInnerBound;
        Range2DRect OuterBound = RoomManager.Instance.ObstaclesOuterBound;
        //vWaypointArea pathArea = GameManager.instance.WaypointArea.GetComponent<vWaypointArea>();

        GameObject enemiesContainer = new GameObject("enemies");
        enemiesContainer.transform.SetParent(roomExtraObjectsContainer.transform);
        GameObject obstaclesContainer = new GameObject("obstacles");
        obstaclesContainer.transform.SetParent(roomExtraObjectsContainer.transform);

        if (PhotonNetwork.IsMasterClient) {
            GameObject[] obstacles = Resources.LoadAll<GameObject>("Prefabs/Environments/Rocks");
            obstacles.Shuffle(0, obstacles.Length);
            int enemyCount = Random.Range(1, enemyCountLimit);
            int obstacleCount = Random.Range(obstacles.Length / 2, obstacles.Length);
            object[] enemyViewIds = new object[enemyCount];
            string[] obstaclePaths = new string[obstacleCount];
            Vector3[] obstaclePositions = new Vector3[obstacleCount];

            for (int i = 0; i < enemyCount; i++) {
                GameObject enemy = PhotonNetwork.InstantiateSceneObject("Prefabs/Enemies/Skeleton_Slave_01", enemiesSpawns[i].position, Quaternion.Euler(enemiesSpawns[i].rotation), 0, null);
                // enemy.GetComponent<v_AIMotor>().pathArea = pathArea;
                enemy.SetActive(false);
                enemyList.Add(enemy);
                enemy.transform.SetParent(enemiesContainer.transform);
                enemyViewIds[i] = enemy.GetComponent<PhotonView>().ViewID;
            }

            for (int i = 0; i < obstacleCount; i++) {
                GameObject obstacle = GameObject.Instantiate<GameObject>(obstacles[i]);
                obstacle.transform.position = GetRandomPositionInRange(InnerBound.GetBorder(), OuterBound.GetBorder());
                obstaclePaths[i] = "Prefabs/Environments/Rocks/" + obstacles[i].name;
                obstaclePositions[i] = obstacle.transform.position;
                obstacle.SetActive(false);
                obstacleList.Add(obstacle);
                obstacle.transform.SetParent(obstaclesContainer.transform);
            }

            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
            roomProperties[GameManager.enemyListKey + roomNumber] = enemyViewIds;
            roomProperties[GameManager.obstaclePathKey + roomNumber] = obstaclePaths;
            roomProperties[GameManager.obstaclePosKey + roomNumber] = obstaclePositions;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        } else {
            ExitGames.Client.Photon.Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
            object[] enemyViewIds = (object[])roomProperties[GameManager.enemyListKey + roomNumber];
            string[] obstaclePaths = (string[])roomProperties[GameManager.obstaclePathKey + roomNumber];
            Vector3[] obstaclePos = (Vector3[])roomProperties[GameManager.obstaclePosKey + roomNumber];
            if (enemyViewIds != null) {
                foreach (int viewID in enemyViewIds) {
                    if (viewID == -1) break;
                    if (PhotonNetwork.GetPhotonView(viewID)) {
                        GameObject enemy = PhotonNetwork.GetPhotonView(viewID).gameObject;
                        enemyList.Add(enemy);
                        // enemy.GetComponent<v_AIMotor>().pathArea = pathArea;
                        enemy.SetActive(false);
                        enemyList.Add(enemy);
                        enemy.GetComponent<Transform>().SetParent(enemiesContainer.GetComponent<Transform>());
                    }
                }
            }

            if (obstaclePaths != null) {
                for (int i = 0; i < obstaclePaths.Length; i++) {
                    GameObject prefab = Resources.Load(obstaclePaths[i]) as GameObject;
                    GameObject obstacle = GameObject.Instantiate(prefab, obstaclePos[i], Quaternion.identity);
                    obstacle.SetActive(false);
                    obstacleList.Add(obstacle);
                    obstacle.transform.SetParent(obstaclesContainer.transform);
                }
            }
        }
    }

    public bool Empty() {
        return roomType == RoomType.EmptyRoom;
    }

    public void SetRoomObjectsActive(bool value) {
        if (enemyList != null) {
            foreach (var enemy in enemyList) {
                enemy.SetActive(value);
            }
        }
        if (itemList != null) {
            foreach (var item in itemList) {
                item.SetActive(value);
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
