using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Invector.vCharacterController;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class UnityEventInt : UnityEvent<int>{}

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public Button exitGameButton;

    public Button minimapButton;
    public Button[] minimapDirButtons;

    public GameObject mainMenu;
    public GameObject minimap;
    public GameObject roomBtnPrefab;
    public GameObject minimapMenu;
    public Text actionText;
    public Text countDownText;
    public Text readyListText;
    public Toggle readyToggle;
    public Toggle gameStartToggle;
    public Slider gameProgress;
    public Text timeRemain;

    private Dictionary<RoomType, Sprite> mapSprite = new Dictionary<RoomType, Sprite>();
    private Image[] mapGrid = new Image[] { };
    private Stack<GameObject> menuStack = new Stack<GameObject>();
    private int selectRoom;

    private void Awake() {
        instance = this;
        selectRoom = -1;
        mapSprite[RoomType.BattleRoom] = Resources.Load<Sprite>("Sprites/Icons/sword");
        mapSprite[RoomType.BossRoom] = Resources.Load<Sprite>("Sprites/Icons/scroll");
        mapSprite[RoomType.RewardRoom] = Resources.Load<Sprite>("Sprites/Icons/coins");
        mapSprite[RoomType.StartingRoom] = Resources.Load<Sprite>("Sprites/Icons/helmets");
        mapSprite[RoomType.EmptyRoom] = Resources.Load<Sprite>("Sprites/Icons/frame");
    }

    private void Start(){
        MessageCenter.Instance.AddEventListener(GLEventCode.DisplayInteractable, ChangeActionText);
        MessageCenter.Instance.AddEventListener(GLEventCode.StartRoomTransition, OnRoomTransitionStart);
        MessageCenter.Instance.AddEventListener(GLEventCode.UpdateEntranceCountDown, OnCountDownUpdate);
        MessageCenter.Instance.AddEventListener(GLEventCode.UpdateReadyList, OnReadyListUpdate);
        MessageCenter.Instance.AddEventListener(GLEventCode.GameStartToggleUpdate, OnGameStartToggleUpdate);

        MessageCenter.Instance.AddObserver(NetEventCode.GameStart, OnGameStart);
        MessageCenter.Instance.AddObserver(NetEventCode.EnterReadyStage, OnEnterReadyStage);
        MessageCenter.Instance.AddObserver(NetEventCode.CancelEnterRoomCountDown, OnCountDownCancel);
        MessageCenter.Instance.AddObserver(NetEventCode.MoveRoom, MoveRoomMinimap);
        MessageCenter.Instance.AddObserver(NetEventCode.GameDurationUpdate, OnDurationUpdate);

        exitGameButton.onClick.AddListener(delegate (){
            OnClick(exitGameButton.gameObject);
        });
        minimapButton.onClick.AddListener(delegate (){
            OnClick(minimapButton.gameObject);
        });

        if(minimapDirButtons.Length != 4) {
            Debug.LogError("Size of minimapDirButtons is not 4! ");
            return;
        }
        minimapDirButtons[0].onClick.AddListener(() => { SendMoveRoomNetEvent(selectRoom, Direction.Down); });
        minimapDirButtons[1].onClick.AddListener(() => { SendMoveRoomNetEvent(selectRoom, Direction.Left); });
        minimapDirButtons[2].onClick.AddListener(() => { SendMoveRoomNetEvent(selectRoom, Direction.Right); });
        minimapDirButtons[3].onClick.AddListener(() => { SendMoveRoomNetEvent(selectRoom, Direction.Up); });

        readyToggle.onValueChanged.AddListener(delegate (bool value) {
            // update ready only when game is not started
            if (!(bool)RoomPropManager.Instance.GetProp(RoomPropType.GameStart)) {
                Hashtable hashtable = new Hashtable(){ { RoomPropManager.PlayerPropKeys[PlayerPropType.Ready], value } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
                MessageCenter.Instance.PostNetEvent2All(NetEventCode.UpdateReady, value);
            }
        });

        gameStartToggle.onValueChanged.AddListener(delegate (bool value) {
            if (value && !(bool)RoomPropManager.Instance.GetProp(RoomPropType.GameStart)) {
                RoomPropManager.Instance.SetProp(RoomPropType.GameStart, true);
                MessageCenter.Instance.PostNetEvent2All(NetEventCode.GameStart);
            }
        });

        OnEnterReadyStage(null);
    }

    private void SendMoveRoomNetEvent(int roomIdx, Direction dir) {
        object[] content = { roomIdx, dir };
        MessageCenter.Instance.PostNetEvent2All(NetEventCode.MoveRoom, content);
    }

    private void OnClick(GameObject go)
    {
        if (go == exitGameButton.gameObject)
        {
            // TODO: PERMISSION: can add listener to CONFIRM button and invoke later
            PhotonNetwork.LeaveRoom();
        } else if(go == minimapButton.gameObject) {
            HideAndActive(minimapMenu);
        } else if(go.name.StartsWith("room")) {
            Debug.Log("room clicked" + go.name);
            selectRoom = int.Parse(go.name.Substring("room".Length));
            UpdateDirBtns(selectRoom);
        }
    }

    private void UpdateDirBtns(int select) {
        bool[] available = RoomManager.Instance.SiblingCheck(selectRoom);
        for (int i = 0; i < available.Length; i++) {
            minimapDirButtons[i].gameObject.SetActive(available[i]);
        }
    }

    public void MoveRoomMinimap(object data){
        object[] content = (object[])data;
        int roomIdx = (int)content[0];
        Direction dir = (Direction)content[1];

        int moveTo = roomIdx + RoomManager.Instance.GetPace(roomIdx, dir);
        Sprite save = mapGrid[moveTo].sprite;
        mapGrid[moveTo].sprite = mapGrid[roomIdx].sprite;
        mapGrid[roomIdx].sprite = save;

        UpdateDirBtns(selectRoom);
    }

    public void HideAndActive(GameObject menu){
        if(menuStack.Count != 0) menuStack.Peek().SetActive(false);
        menuStack.Push(menu);
        menu.SetActive(true);
    }

    /// <summary>
    /// Draw map of the dungeon on GUI minimap
    /// </summary>
    public void BuildMinimap(object[] roomtypes){
        // map rebuid situation
        if(mapGrid.Length != 0) {
            foreach (Image image in mapGrid) {
                if(image.gameObject != null) {
                    Destroy(image.gameObject);
                }
            }
        }

        int mapSize = (int)RoomPropManager.Instance.GetProp(RoomPropType.MapSize);
        mapGrid = new Image[mapSize * mapSize];
        RectTransform minimapRT = minimap.GetComponent<RectTransform>();
        float width = minimapRT.rect.width / mapSize;
        float height = minimapRT.rect.height / mapSize;
        float posX = - minimapRT.rect.width / 2 + width / 2;
        float posY = - minimapRT.rect.width / 2 + width / 2;
        // Debug.LogFormat("width:{0}, height:{1}, posX:{2}, posY:{3}", width, height, posX, posY);
        for (int i = 0; i < mapSize; i++) {
            for (int j = 0; j < mapSize; j++) {
                int roomIdx = mapSize * i + j;
                GameObject roomBtn = Instantiate(roomBtnPrefab, minimapRT) as GameObject;
                roomBtn.name = "room" + roomIdx;
                roomBtn.transform.localScale = new Vector3(width / 100f, height / 100f, 0f) * 0.7f;

                roomBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(posX + width * j, posY + height * i);
                RoomType type = (RoomType)roomtypes[roomIdx];
                if(type != RoomType.EmptyRoom) {
                    roomBtn.GetComponent<Image>().sprite = mapSprite[type];
                }
                roomBtn.GetComponent<Button>().onClick.AddListener(()=> { OnClick(roomBtn); });
                mapGrid[roomIdx] = roomBtn.GetComponent<Image>();
            }
        }
    }
    
    public void ChangeActionText(object message){
        actionText.text = (string)message;
    }

    private void OnGameStartToggleUpdate(object data) {
        gameStartToggle.interactable = (bool)data;
    }

    /// <summary>
    /// 接收到城主发出开始游戏的通知
    /// </summary>
    /// <param name="data"></param>
    private void OnGameStart(object data) {
        readyListText.gameObject.SetActive(false);
        readyToggle.gameObject.SetActive(false);
        gameStartToggle.gameObject.SetActive(false);
    }

    /// <summary>
    /// 进入一局游戏的准备阶段
    /// </summary>
    /// <param name="data"></param>
    public void OnEnterReadyStage(object data) {
        // 绘制房间地图
        object[] roomTypes = (object[])RoomPropManager.Instance.GetProp(RoomPropType.GridMap);
        BuildMinimap(roomTypes);

        readyListText.gameObject.SetActive(true);
        // 玩家重新进入可准备状态
        readyToggle.isOn = false;
        readyToggle.gameObject.SetActive(true);
        // 城主重新进入可开始状态
        if (PhotonNetwork.IsMasterClient) {
            gameStartToggle.isOn = false;
            gameStartToggle.interactable = false;
            gameStartToggle.gameObject.SetActive(true);
        }
        gameProgress.value = 0f;
        timeRemain.text = GetTimeFormat(0);
    }

    private void OnCountDownUpdate(object data) {
        string _text = ((float)data).ToString();
        if (countDownText) {
            countDownText.text = _text;
        }
    }

    private void OnRoomTransitionStart(object data) {
        countDownText.text = string.Empty;
        actionText.text = string.Empty;
        // might have other operations
    }

    private void OnReadyListUpdate(object data) {
        object[] _data = (object[])data;
        string[] nicknames = (string[])_data[0];
        bool[] readys = (bool[])_data[1];
        string content = "";
        for(int i = 0; i < nicknames.Length; i++) {
            if (readys[i]) {
                content += string.Format("Player {0} is ready\n", nicknames[i]);
            } else {
                content += string.Format("Player {0} is not ready\n", nicknames[i]);
            }
        }
        readyListText.text = content;
    }

    private void OnCountDownCancel(object data) {
        countDownText.text = string.Empty;
    }

    private string GetTimeFormat(int remain) {
        int min = remain / 60, sec = remain % 60;
        string minute, second;
        if (min >= 10) {
            minute = (min).ToString();
        } else if (min < 10 && min > 0) {
            minute = "0" + (min).ToString();
        } else {
            minute = "00";
        }
        if (sec >= 10) {
            second = (sec).ToString();
        } else if (sec < 10 && sec > 0) {
            second = "0" + (sec).ToString();
        } else {
            second = "00";
        }
        return string.Format("{0} : {1}", minute, second);
    }

    private void OnDurationUpdate(object data) {
        int remain = (int)data;
        //Debug.Log("[UIManager] received remain: " + remain);
        gameProgress.value = 1.0f - (float)remain / GameManager.Instance.gameDuration;
        timeRemain.text = GetTimeFormat(remain);
    }

    void Update()
    {
        if(Input.GetButtonDown("Escape")){
            // Debug.Log("MUL: Escape pressed!");
            if (menuStack.Count == 0) {
                HideAndActive(mainMenu);
                // unlock cursor from the centor of screen, show cursor and lock all input(basic and melee)
                MessageCenter.Instance.PostGLEvent(GLEventCode.EnterBrowseMode);
            } else {
                menuStack.Peek().SetActive(false);
                menuStack.Pop();
                if (menuStack.Count != 0) {
                    menuStack.Peek().SetActive(true);
                } else {
                    // lock cursor again, hide cursor and unlock all input(basic and melee)
                    MessageCenter.Instance.PostGLEvent(GLEventCode.ExitBrowseMode);
                }
            }
        }    
    }
}
