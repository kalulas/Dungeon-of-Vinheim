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
using Hashtable = ExitGames.Client.Photon.Hashtable;

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
    UpdateReady,
    GameStart,
    StartEnterRoomCountDown,
    CancelEnterRoomCountDown,
    MoveRoom,
    GameClear,
    EnterReadyStage,
    GameDurationUpdate,
    End,
}

public enum GLEventCode {
    Start,
    UpdateReadyList,
    GameStartToggleUpdate,
    UpdateEntranceCountDown,
    StartRoomTransition,
    EndRoomTransition,
    EnterBrowseMode,
    ExitBrowseMode,
    DisplayInteractable,
    DisplayFadeText,
    DisplayFadeTextWithConfig,
    PlayAnimation,
    ResetAnimation,
    End,
}

public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback {
    public static GameManager Instance;
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject localPlayer;
    [HideInInspector]
    public TimeConfig timeConfig;
    public string hostInstancePath;
    public string memberInstancePath;
    [HideInInspector]
    public int gameDuration;

    private Coroutine entranceCountDownCor = null;
    private Coroutine entranceEnterCor = null;

    private LoadObject[] playerSpawns;
    Coroutine hostGameCountDown;

    #region Photon Callbacks
    /// <summary>
    /// Called when the local player left the room. We need to load the launcher scene.
    /// </summary>
    /// 

    public override void OnLeftRoom() {
        StartCoroutine(LoadLauncher());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) {
        MessageCenter.Instance.PostGLEvent(GLEventCode.DisplayFadeText, string.Format("Player {0} has entered this dungeon", newPlayer.NickName));
        // or you can sent GLEvent
        OnReadyUpdate(null);
    }

    public void OnEvent(EventData photonEvent) {
        //Debug.LogFormat("Received photonEvent with EventCode:{0}", photonEvent.Code);
        if(photonEvent.Code > (byte)NetEventCode.PlaceHolder && photonEvent.Code < (byte)NetEventCode.End) {
            MessageCenter.Instance.NetEventDataQueue.Enqueue(photonEvent);
        }
    }

    public override void OnPlayerLeftRoom(Player newPlayer) {
        MessageCenter.Instance.PostGLEvent(GLEventCode.DisplayFadeText, string.Format("Player {0} has left this dungeon", newPlayer.NickName));
        OnReadyUpdate(null);
    }

    #endregion

    IEnumerator LoadLauncher() {
        AsyncOperation async = SceneManager.LoadSceneAsync(0);
        async.allowSceneActivation = false;
        float wait = 0f;
        while (!async.isDone) {
            if(wait < timeConfig.LoadSceneWait) {
                wait += Time.deltaTime;
            } else {
                async.allowSceneActivation = true;
            }
            yield return null;
        }
    }

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
        gameDuration = (int)RoomPropManager.Instance.GetProp(RoomPropType.Duration);

        if (PhotonNetwork.IsMasterClient) {
            RoomManager.Instance.SetUpRoomMap();
        }
    }

    private void OnDestroy() {
        MessageCenter.ReleaseInstance();
        RoomManager.ReleaseInstance();
        ActionManager.ReleaseInstance();
        QueueManager.ReleaseInstance();
        RoomPropManager.ReleaseInstance();
        AnimationManager.ReleaseInstance();
    }

    private void Start() {
        MessageCenter.Instance.AddEventListener(GLEventCode.EndRoomTransition, OnRoomTransitionEnd);

        MessageCenter.Instance.AddObserver(NetEventCode.UpdateReady, OnReadyUpdate);
        MessageCenter.Instance.AddObserver(NetEventCode.StartEnterRoomCountDown, OnCountDownStart);
        MessageCenter.Instance.AddObserver(NetEventCode.CancelEnterRoomCountDown, OnCountDownCancel);
        MessageCenter.Instance.AddObserver(NetEventCode.EnterRoom, OnEnterRoom);
        MessageCenter.Instance.AddObserver(NetEventCode.GameClear, OnGameClear);
        MessageCenter.Instance.AddObserver(NetEventCode.GameStart, OnGameStart);
        MessageCenter.Instance.AddObserver(NetEventCode.EnterReadyStage, BuildDungeonAndLoadPlayer);

        BuildDungeonAndLoadPlayer(null);
        OnReadyUpdate(null);
    }

    private void OnReadyUpdate(object data) {
        Player[] players = PhotonNetwork.PlayerList;
        string[] nicknames = new string[players.Length];
        bool[] readys = new bool[players.Length];
        bool allReady = true;
        for (int i = 0; i < players.Length; i++) {
            bool ready = false;
            if (players[i].CustomProperties.ContainsKey(RoomPropManager.PlayerPropKeys[PlayerPropType.Ready])) {
                ready = (bool)players[i].CustomProperties[RoomPropManager.PlayerPropKeys[PlayerPropType.Ready]];
            }
            nicknames[i] = players[i].NickName;
            readys[i] = ready;
            allReady &= ready;
        }
        if (PhotonNetwork.IsMasterClient) {
            if (allReady) {
                // enable start button
                MessageCenter.Instance.PostGLEvent(GLEventCode.GameStartToggleUpdate, true);
            } else {
                MessageCenter.Instance.PostGLEvent(GLEventCode.GameStartToggleUpdate, false);
            }
        }
        object[] _data = new object[2] { nicknames, readys };
        MessageCenter.Instance.PostGLEvent(GLEventCode.UpdateReadyList, _data);
    }

    /// <summary>
    /// 进入准备阶段：城主加载地图生成需要同步的对象
    /// 其他玩家根据城主设置的房间内容建设房间
    /// </summary>
    /// <param name="data"></param>
    private void BuildDungeonAndLoadPlayer(object data) {
        object[] roomTypes = (object[])RoomPropManager.Instance.GetProp(RoomPropType.GridMap);
        RoomManager.Instance.BuildAndCreateMap(roomTypes);

        // load & justify player position for the first time
        // 根据玩家在游戏内的ID编号决定加载位置
        int load = 5 * (int)Direction.Center + GetPlayerIndex();
        if (localPlayer == null) {
            if (PhotonNetwork.IsMasterClient) {
                localPlayer = PhotonNetwork.Instantiate(memberInstancePath, playerSpawns[load].position, Quaternion.Euler(playerSpawns[load].rotation));
            } else {
                localPlayer = PhotonNetwork.Instantiate(memberInstancePath, playerSpawns[load].position, Quaternion.Euler(playerSpawns[load].rotation));
            }
        } 
        // reposition local player and reset properties
        localPlayer.transform.position = playerSpawns[load].position;
        localPlayer.transform.rotation = Quaternion.Euler(playerSpawns[load].rotation);
        // TODO: 重置玩家属性，禁止输入等待等等
        localPlayer.GetComponent<vThirdPersonController>().ResetHealth();
        MessageCenter.Instance.PostGLEvent(GLEventCode.EnterBrowseMode);
    }

    /// <summary>
    /// 城主通知游戏开始
    /// </summary>
    /// <param name="data"></param>
    private void OnGameStart(object data) {
        Debug.Log("[GameManager] GAME START!");
        if (PhotonNetwork.IsMasterClient) {
            RoomManager.Instance.GenerateAllSceneObjects();
            // 进行游戏倒计时，若倒计时结束仍未击杀boss则城主胜利
            int remain = gameDuration;
            hostGameCountDown = WaitTimeManager.CreateCoroutine(true, gameDuration, delegate () {
                remain--;
                MessageCenter.Instance.PostNetEvent2All(NetEventCode.GameDurationUpdate, remain);
                if (remain == 0) {
                    // 城主胜利
                    MessageCenter.Instance.PostNetEvent2All(NetEventCode.GameClear, true);
                }
            }, 1.0f, true);
        }
        //RoomManager.Instance.GetAllRoomsReady();
        MessageCenter.Instance.PostGLEvent(GLEventCode.ExitBrowseMode);
    }

    /// <summary>
    /// 一局游戏结束
    /// 城主提前生成好下一局地图，加载完毕后通知其他主机进入准备阶段
    /// </summary>
    /// <param name="data">城主胜利为true，侵入者胜利为false</param>
    private void OnGameClear(object data) {
        if (PhotonNetwork.IsMasterClient) {
            // 结束城主倒计时
            if(hostGameCountDown != null) {
                WaitTimeManager.CancelCoroutine(ref hostGameCountDown);
            }
            RoomManager.Instance.SetUpRoomMap();
            WaitTimeManager.CreateCoroutine(false, timeConfig.GameInternal, delegate () {

                // 取消所有准备，置开始为false，再通知其他主机
                Player[] players = PhotonNetwork.PlayerList;
                Hashtable hashtable = new Hashtable() { { RoomPropManager.PlayerPropKeys[PlayerPropType.Ready], false } };
                foreach (Player player in players) {
                    player.SetCustomProperties(hashtable);
                }
                RoomPropManager.Instance.SetProp(RoomPropType.GameStart, false);
                MessageCenter.Instance.PostNetEvent2All(NetEventCode.EnterReadyStage);
            });
        }
        string gameClearMessage;
        if(!(PhotonNetwork.IsMasterClient ^ (bool)data)) {
            gameClearMessage = "You win! Congratulations!";
        } else {
            gameClearMessage = "Sorry, maybe next time.";
        }
        float displayTime = timeConfig.GameInternal;
        float fadeTime = 0.5f;
        object[] config = new object[]{ gameClearMessage, displayTime, fadeTime };
        MessageCenter.Instance.PostGLEvent(GLEventCode.DisplayFadeTextWithConfig, config);
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

    // adjust the loading position of the player
    private int GetPlayerIndex() {
        Player[] players = PhotonNetwork.PlayerList;
        int total = players.Length;
        int idx = 0;
        for (int i = 0; i < total; i++) {
            if(players[i].ActorNumber < PhotonNetwork.LocalPlayer.ActorNumber) {
                idx++;
            }
        }
        return idx;
    }

    private void OnRoomTransitionEnd(object data) {
        Direction direction = (Direction)data;
        // 5 positions each direction ->  5 * direction + playerIndex
        localPlayer.transform.position = playerSpawns[5 * (3 - (int)direction) + GetPlayerIndex()].position;
        localPlayer.transform.rotation = Quaternion.Euler(playerSpawns[5 * (3 - (int)direction) + GetPlayerIndex()].rotation);
    }
}
