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

public enum NetEventCode {
    PlaceHolder,
    EnterRoom,
    StartEnterRoomCountDown,
    CancelEnterRoomCountDown,
    MoveRoom,
    End,
}

public enum GLEventCode {
    Start,
    UpdateEntranceCountDown,
    StartRoomTransition,
    EndRoomTransition,
    EnterBrowseMode,
    ExitBrowseMode,
    DisplayInteractable,
    DisplayFadeText,
    PlayAnimation,
    ResetAnimation,
    End,
}

public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback {
    public static GameManager Instance;
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject localPlayer;
    public TimeConfig timeConfig;
    public string hostInstancePath;
    public string memberInstancePath;

    private Coroutine entranceCountDownCor = null;
    private Coroutine entranceEnterCor = null;

    private LoadObject[] playerSpawns;

    #region Photon Callbacks
    /// <summary>
    /// Called when the local player left the room. We need to load the launcher scene.
    /// </summary>
    public override void OnLeftRoom() {
        SceneManager.LoadScene(0,LoadSceneMode.Single);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", newPlayer.NickName); // not seen if you're the player connecting
        if (PhotonNetwork.IsMasterClient) {
            Debug.LogFormat("OnPlayerEnteredRoom(): update {0}'s map", newPlayer); // called before OnPlayerLeftRoom
        }
    }

    public void OnEvent(EventData photonEvent) {
        Debug.LogFormat("Received photonEvent with EventCode:{0}", photonEvent.Code);
        if(photonEvent.Code > (byte)NetEventCode.PlaceHolder && photonEvent.Code < (byte)NetEventCode.End) {
            MessageCenter.Instance.NetEventDataQueue.Enqueue(photonEvent);
        }
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

    private void Awake() {
        Instance = this;
        MessageCenter.CreateInstance();
        RoomManager.CreateInstance();
        ActionManager.CreateInstance();
        QueueManager.CreateInstance();
        RoomPropManager.CreateInstance();
        AnimationManager.CreateInstance();

        LoadPositionConfig config = Resources.Load("Configs/RoomPositionConfig") as LoadPositionConfig;
        playerSpawns = new LoadObject[config.loads.Count];
        config.loads.CopyTo(playerSpawns);
        timeConfig = Resources.Load("Configs/DungeonTimeConfig") as TimeConfig;

        if (PhotonNetwork.IsMasterClient) {
            object[] roomtypes = RoomManager.Instance.SetUpRoomMap();
            RoomPropManager.Instance.SetProp(RoomPropType.GridMap, roomtypes);
        }
    }

    private void Start() {
        MessageCenter.Instance.AddEventListener(GLEventCode.EndRoomTransition, OnRoomTransitionEnd);

        MessageCenter.Instance.AddObserver(NetEventCode.StartEnterRoomCountDown, OnCountDownStart);
        MessageCenter.Instance.AddObserver(NetEventCode.CancelEnterRoomCountDown, OnCountDownCancel);
        MessageCenter.Instance.AddObserver(NetEventCode.EnterRoom, OnEnterRoom);

        object[] roomTypes = (object[])RoomPropManager.Instance.GetProp(RoomPropType.GridMap);
        RoomManager.Instance.BuildAndCreateMap(roomTypes);
        UIManager.instance.BuildMinimap(roomTypes);

        // load & justify player position
        if (localPlayer == null) {
            Debug.LogFormat("We are Instantiating LocalPlayer from {0}", Application.loadedLevelName);
            int load = (int)Direction.Center;
            if (PhotonNetwork.IsMasterClient) {
                localPlayer = PhotonNetwork.Instantiate(hostInstancePath, playerSpawns[load].position, Quaternion.Euler(playerSpawns[load].rotation), 0);
            } else {
                localPlayer = PhotonNetwork.Instantiate(memberInstancePath, playerSpawns[load].position, Quaternion.Euler(playerSpawns[load].rotation), 0);
            }
        }
    }

    private void OnCountDownStart(object data) {
        Direction dir = (Direction)data;
        float countDown = timeConfig.EntranceQueueWait;
        entranceCountDownCor = WaitTimeManager.CreateCoroutine(true, timeConfig.EntranceQueueWait, delegate () {
            countDown -= 1.0f;
            MessageCenter.Instance.PostGLEvent(GLEventCode.UpdateEntranceCountDown, countDown);
        }, 1.0f, true);

        entranceEnterCor = WaitTimeManager.CreateCoroutine(false, timeConfig.EntranceQueueWait, delegate () {
            MessageCenter.Instance.PostGLEvent(GLEventCode.StartRoomTransition, data);
        });
    }

    private void OnCountDownCancel(object data) {
        Direction dir = (Direction)data;
        WaitTimeManager.CancelCoroutine(ref entranceEnterCor);
        WaitTimeManager.CancelCoroutine(ref entranceCountDownCor);
    }

    private void OnEnterRoom(object data) {
        Direction dir = (Direction)data;
        WaitTimeManager.CancelCoroutine(ref entranceEnterCor);
        WaitTimeManager.CancelCoroutine(ref entranceCountDownCor);
        MessageCenter.Instance.PostGLEvent(GLEventCode.StartRoomTransition, data);
    }

    private void OnRoomTransitionEnd(object data) {
        Direction direction = (Direction)data;
        localPlayer.transform.position = playerSpawns[3 - (int)direction].position;
        localPlayer.transform.rotation = Quaternion.Euler(playerSpawns[3 - (int)direction].rotation);
    }
}
