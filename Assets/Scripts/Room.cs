using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Invector.vCharacterController.AI;

using Photon.Pun;

public enum RoomType
{
    EmptyRoom,
    StartingRoom,
    BattleRoom,
    RewardRoom,
    BossRoom
}

// Now GameManager can create instance of ScriptableObject Room
namespace DungeonOfVinheim{
    using ExitGames.Client.Photon;
    [CreateAssetMenu]
    public class Room : ScriptableObject
    {
        // private int roomIndex;
        public static GameObject roomsContainer;
        [SerializeField]
        private int roomNumber;
        public RoomType roomType{ get; private set; }

        public bool setup{ get; private set; } = false;
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
        // [SerializeField]
        // private int[] enemyViewIdList = new int[enemyCountLimit]{-1,-1,-1,-1,-1,-1,-1,-1,-1};
        [SerializeField]
        private List<GameObject> obstacleList = new List<GameObject>();
        [SerializeField]
        private List<GameObject> itemList = new List<GameObject>();

        private Vector3 GetRandomPositionInRange(List<Transform> points){
            // points:
            // | 1 - - - | 1: OuterTopLeft 
            // | - 3 - - | 3: InnerTopLeft 
            // | - - 4 - | 4: InnerDownRight
            // | - - - 2 | 2: OuterDownRight
            float x = Random.Range(points[1].position.x, points[2].position.x);
            float z = 0f;
            if(x < points[3].position.x || x > points[4].position.x){
                z = Random.Range(points[1].position.z, points[2].position.z);
            }
            else{
                float z1 = Random.Range(points[1].position.z, points[3].position.z);
                float z2 = Random.Range(points[4].position.z, points[2].position.z);
                z = Random.Range(0f, 1f) < 0.5 ? z1 : z2;
            }
            return new Vector3(x, 0f, z);
        }

        public Room SetUp(int number, RoomType type, GameObject extra=null){
            roomType = type;
            roomExtra = extra;
            roomNumber = number;
            roomExtraObjectsContainer = new GameObject("room" + roomNumber + roomType);
            roomExtraObjectsContainer.GetComponent<Transform>().SetParent(roomsContainer.GetComponent<Transform>());
            if (PhotonNetwork.IsMasterClient)
            {
                // TODO: create enemies & items here
                switch (type)
                {
                    case RoomType.BattleRoom: SetUpBattleRoom(); break;
                    default: break;
                }
                setup = true;
            }
            itemList = null;
            return this;
        }

        public void GetReady(){
            switch (roomType)
            { 
                case RoomType.BattleRoom: SetUpBattleRoom(); break;
                default: break;
            }
            setup = true;
        }

        private void SetUpBattleRoom(){
            List<Transform> enemiesSpawnPoints = GameManager.instance.enemiesSpawnPoints;
            List<Transform> points = GameManager.instance.obstaclesSpawnPoints;
            vWaypointArea pathArea = GameManager.instance.WaypointArea.GetComponent<vWaypointArea>();
            GameObject enemiesContainer = new GameObject("enemies");
            enemiesContainer.GetComponent<Transform>().SetParent(roomExtraObjectsContainer.GetComponent<Transform>());

            if (PhotonNetwork.IsMasterClient)
            {
                int enemyCount = Random.Range(1, enemyCountLimit);
                object[] enemyViewIdList = new object[enemyCount];
                for (int i = 0; i < enemyCount; i++)
                {
                    GameObject enemy = PhotonNetwork.InstantiateSceneObject("Prefabs/Enemies/Skeleton_Slave_01", enemiesSpawnPoints[i + 1].position, Quaternion.identity, 0, null);
                    // GameObject enemy = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/Enemies/Skeleton_Slave_01"), enemiesSpawnPoints[i+1]);
                    enemy.GetComponent<v_AIMotor>().pathArea = pathArea;
                    enemy.SetActive(false);
                    enemyList.Add(enemy);
                    enemy.GetComponent<Transform>().SetParent(enemiesContainer.GetComponent<Transform>());
                    enemyViewIdList[i] = enemy.GetComponent<PhotonView>().ViewID;
                }
                Hashtable roomProperties = new Hashtable();
                roomProperties[GameManager.enemyListKey + roomNumber] = enemyViewIdList;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
            }
            else{
                Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
                object[] enemyViewIdList = (object[])roomProperties[GameManager.enemyListKey + roomNumber];
                if (enemyViewIdList != null)
                {
                    foreach (int viewID in enemyViewIdList)
                    {
                        if (viewID == -1) break;
                        if (PhotonNetwork.GetPhotonView(viewID))
                        {
                            GameObject enemy = PhotonNetwork.GetPhotonView(viewID).gameObject;
                            enemyList.Add(enemy);
                            enemy.GetComponent<v_AIMotor>().pathArea = pathArea;
                            enemy.SetActive(false);
                            enemyList.Add(enemy);
                            enemy.GetComponent<Transform>().SetParent(enemiesContainer.GetComponent<Transform>());
                        }
                    }
                }
            }

            GameObject[] obstacles = Resources.LoadAll<GameObject>("Prefabs/Environments/Rocks");
            GameObject obstaclesContainer = new GameObject("obstacles");
            obstaclesContainer.GetComponent<Transform>().SetParent(roomExtraObjectsContainer.GetComponent<Transform>());
            // shuffle and random number
            // NOTE: EDIT OBSTACLES NUMBER HERE
            obstacles.Shuffle(0, obstacles.GetLength(0));
            int obstacleCount = Random.Range(1, obstacles.GetLength(0));
            for (int i = 0; i < obstacleCount; i++)
            {
                GameObject obstacle = GameObject.Instantiate<GameObject>(obstacles[i]);
                obstacle.GetComponent<Transform>().position = GetRandomPositionInRange(points);
                obstacle.SetActive(false);
                obstacleList.Add(obstacle);
                obstacle.GetComponent<Transform>().SetParent(obstaclesContainer.GetComponent<Transform>());
            }
        }

        public bool Empty(){
            return roomType == RoomType.EmptyRoom;
        }

        public void SetRoomObjectsActive(bool value){
            if (enemyList != null)
            {
                foreach (var enemy in enemyList) enemy.SetActive(value);
            }
            if (itemList != null)
            {
                foreach (var item in itemList) item.SetActive(value);
            }
        }

        public void SetRoomEnvActive(bool value){
            // rotate the content of the boss room, so player will be in the front of the boss
            // iN fact the player can only enter from direction "down" or "left"
            if(value){
                if(roomType == RoomType.BossRoom){
                    float angleY = GameManager.localPlayerInstance.GetComponent<Transform>().position == GameManager.positions[0] ? 180 : 270;
                    roomExtra.transform.rotation = Quaternion.Euler(0, angleY, 0);
                }
            }
            if(obstacleList != null){
                foreach (var obstacle in obstacleList) obstacle.SetActive(value);
            }
            roomExtra.SetActive(value);
        }
    }
}
