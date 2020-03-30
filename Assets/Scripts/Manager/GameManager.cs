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
using ExitGames.Client.Photon;

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

public enum EventCode {
    PlaceHolder,
    EnterRoom,
    StartEnterRoomCountDown,
    CancelEnterRoomCountDown,
    MoveRoom,
}

public class TriggerEvent : UnityEvent { }

// 单例脚本管理地图信息并进行关卡切换
public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback {
    public static GameManager Instance;
    public static TriggerEvent ActionTriggerEvent = new TriggerEvent();

    public static readonly string playerLocationKey = "roomNumber";
    public static readonly string mapDataKey = "gridMap";
    public static readonly string enemyListKey = "enemyList";
    public static readonly string obstaclePathKey = "obstaclePath";
    public static readonly string obstaclePosKey = "obstaclePos";

    // the same as original vThirdPersonController.LocalPlayerInstance
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject localPlayerInstance;
    public string hostInstancePath;
    public string memberInstancePath;

    private float entranceWaitTime = 5.0f;
    private bool entranceAvailable = true;
    private Coroutine entranceCountDownCor = null;
    private Coroutine entranceEnterCor = null;

    private LoadObject[] playerSpawns;

    #region Photon Callbacks
    /// <summary>
    /// Called when the local player left the room. We need to load the launcher scene.
    /// </summary>
    public override void OnLeftRoom() {
        SceneManager.LoadScene(0);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", newPlayer.NickName); // not seen if you're the player connecting
        if (PhotonNetwork.IsMasterClient) {
            Debug.LogFormat("OnPlayerEnteredRoom(): update {0}'s map", newPlayer); // called before OnPlayerLeftRoom
        }
    }

    // TODO: FIX THIS
    public void OnEvent(EventData photonEvent) {
        byte eventCode = photonEvent.Code;
        switch (eventCode) {
            case (byte)EventCode.EnterRoom:
                WaitTimeManager.CancelCoroutine(ref entranceEnterCor);
                WaitTimeManager.CancelCoroutine(ref entranceCountDownCor);
                UIManager.instance.SetCountDownText("");
                EnterRoom((Direction)photonEvent.CustomData);
                break;
            case (byte)EventCode.StartEnterRoomCountDown:
                Debug.Log("start wait in direction " + (Direction)photonEvent.CustomData);
                float countDown = entranceWaitTime;
                entranceCountDownCor = WaitTimeManager.CreateCoroutine(true, entranceWaitTime, delegate () {
                    countDown -= 1.0f;
                    UIManager.instance.SetCountDownText(countDown.ToString());
                }, 1.0f, true);
                entranceEnterCor = WaitTimeManager.CreateCoroutine(false, entranceWaitTime, delegate () {
                    UIManager.instance.SetCountDownText("");
                    EnterRoom((Direction)photonEvent.CustomData);
                });

                localPlayerInstance.SendMessage("SetLockAllInput", true);
                break;
            case (byte)EventCode.CancelEnterRoomCountDown:
                WaitTimeManager.CancelCoroutine(ref entranceEnterCor);
                WaitTimeManager.CancelCoroutine(ref entranceCountDownCor);
                UIManager.instance.SetCountDownText("");
                Debug.Log("cancel wait in direction " + (Direction)photonEvent.CustomData + " if waited");

                localPlayerInstance.SendMessage("SetLockAllInput", false);
                break;
            case (byte)EventCode.MoveRoom:
                object[] content = (object[])photonEvent.CustomData;
                int selectIdx = (int)content[0];
                Direction dir = (Direction)content[1];
                // first logic then view
                RoomManager.Instance.moveRoomEvent.Invoke(selectIdx, dir);
                UIManager.instance.moveRoomEvent.Invoke(selectIdx, dir);
                break;
            default: break;
        }
    }

    public static void SendNetworkEvent(EventCode eventCode, object content, RaiseEventOptions raiseEventOptions) {
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent((byte)eventCode, content, raiseEventOptions, sendOptions);
    }

    public override void OnPlayerLeftRoom(Player newPlayer) {
        Debug.LogFormat("OnPlayerLeftRoom() {0}", newPlayer.NickName); // seen when other disconnects    
        if (PhotonNetwork.IsMasterClient) {
            Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", newPlayer.IsMasterClient); // called before OnPlayerLeftRoom
        }
    }

    #endregion

    public new void OnEnable() {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public new void OnDisable() {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // Start is called before the first frame update
    void Awake() {
        RoomManager.CreateInstance();
        QueueManager.CreateInstance();
        Instance = this;

        LoadPositionConfig config = Resources.Load("Configs/RoomPositionConfig") as LoadPositionConfig;
        playerSpawns = new LoadObject[config.loads.Count];
        config.loads.CopyTo(playerSpawns);

        if (PhotonNetwork.IsMasterClient) {
            object[] roomtypes = RoomManager.Instance.SetUpRoomMap();
            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable(){
                { mapDataKey, roomtypes },
                { "Down", 0 },
                { "Up", 0 },
                { "Left", 0 },
                { "Right", 0 },
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        }
    }

    private void Start() {
        ExitGames.Client.Photon.Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
        object[] gridMap = (object[])roomProperties[mapDataKey];
        RoomType[] roomTypes = new RoomType[gridMap.Length];
        for (int i = 0; i < gridMap.Length; i++) {
            roomTypes[i] = (RoomType)gridMap[i];
        }
        RoomManager.Instance.BuildAndCreateMap(roomTypes);
        UIManager.instance.BuildMinimap(roomTypes);

        // load & justify player position
        if (localPlayerInstance == null) {
            Debug.LogFormat("We are Instantiating LocalPlayer from {0}", Application.loadedLevelName);
            int load = (int)Direction.Center;
            if (PhotonNetwork.IsMasterClient) {
                localPlayerInstance = PhotonNetwork.Instantiate(hostInstancePath, playerSpawns[load].position, Quaternion.Euler(playerSpawns[load].rotation), 0);
            } else {
                localPlayerInstance = PhotonNetwork.Instantiate(memberInstancePath, playerSpawns[load].position, Quaternion.Euler(playerSpawns[load].rotation), 0);
            }
        }
    }

    /// <summary>
    /// handle triggerevent when the player is trying to enter the next room
    /// </summary>
    public void HandleEntranceEvent(Direction direction) {
        if (!RoomManager.Instance.AvailableAhead(direction)) {
            Debug.Log("BLOCKED");
            UIManager.setActionTextContentEvent.Invoke("BLOCKED", false);
        } else if (entranceAvailable) {
            QueueManager.Instance.QueueAtDirection(direction);
        } else {
            Debug.Log("sorry entrance not available");
        }
    }

    // to be moved
    private void EnterRoom(Direction direction) {
        // same network events may arrive during the process
        entranceAvailable = false;
        // some item on exit cant function properly
        ActionTriggerEvent.RemoveAllListeners();
        GameObject entrance = GameObject.Find("Entrance" + direction.ToString());

        // NOTE: no TRIGGER EXIT event now so manually set action text false
        UIManager.setActionTextActiveEvent.Invoke(false);
        // TODO: play smoke animation & player's open door animation maybe you need to reposition the player
        Animation ani = entrance.GetComponent<Animation>();
        ani.Play("DoorOpen");
        // wait until the entrance is fully open
        ItemTrigger.entranceFullyOpenEvent.AddListener(delegate () {
            RoomManager.Instance.LoadRoomAtDirection(direction);
            localPlayerInstance.transform.position = playerSpawns[3 - (int)direction].position;
            localPlayerInstance.transform.rotation = Quaternion.Euler(playerSpawns[3 - (int)direction].rotation);

            // reset the entrance's animation
            AnimationState state = ani["DoorOpen"];
            state.time = 0;
            ani.Sample();
            state.enabled = false;

            // reset wait count after entered a new room
            ExitGames.Client.Photon.Hashtable roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
            roomProperties["Down"] = 0;
            roomProperties["Left"] = 0;
            roomProperties["Right"] = 0;
            roomProperties["Up"] = 0;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            ItemTrigger.entranceFullyOpenEvent.RemoveAllListeners();
            QueueManager.Instance.SetWaiting(false);
            entranceAvailable = true;
            localPlayerInstance.SendMessage("SetLockAllInput", false);
        });
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetButtonDown("A")) {
            ActionTriggerEvent.Invoke();
        }
    }
}
