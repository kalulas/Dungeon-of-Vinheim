using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Invector.vCharacterController;

using Photon.Pun;
using Photon.Realtime;

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

    private Dictionary<RoomType, Sprite> mapSprite = new Dictionary<RoomType, Sprite>();
    private Image[] mapGrid;
    private Stack<GameObject> menuStack = new Stack<GameObject>();
    private int selectRoom;

    private void Awake() {
        instance = this;
        selectRoom = -1;
        mapSprite[RoomType.BattleRoom] = Resources.Load<Sprite>("Icons/sword");
        mapSprite[RoomType.BossRoom] = Resources.Load<Sprite>("Icons/scroll");
        mapSprite[RoomType.RewardRoom] = Resources.Load<Sprite>("Icons/coins");
        mapSprite[RoomType.StartingRoom] = Resources.Load<Sprite>("Icons/helmets");
        mapSprite[RoomType.EmptyRoom] = Resources.Load<Sprite>("Icons/frame");
    }

    private void Start(){
        MessageCenter.Instance.AddEventListener(GLEventCode.DisplayInteractable, ChangeActionText);
        MessageCenter.Instance.AddEventListener(GLEventCode.StartRoomTransition, OnRoomTransitionStart);
        MessageCenter.Instance.AddEventListener(GLEventCode.UpdateEntranceCountDown, OnCountDownUpdate);
        MessageCenter.Instance.AddObserver(NetEventCode.CancelEnterRoomCountDown, OnCountDownCancel);
        MessageCenter.Instance.AddObserver(NetEventCode.MoveRoom, MoveRoomMinimap);

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
    }

    private void SendMoveRoomNetEvent(int roomIdx, Direction dir) {
        object[] content = { roomIdx, dir };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        MessageCenter.Instance.PostNetEvent(NetEventCode.MoveRoom, content, raiseEventOptions);
    }

    void OnClick(GameObject go)
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
        int mapSize = RoomManager.Instance.MapSize;
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

    private void OnRoomTransitionStart(object data) {
        countDownText.text = string.Empty;
        actionText.text = string.Empty;
        // might have other operations
    }

    public void OnCountDownUpdate(object data) {
        string _text = ((float)data).ToString();
        if (countDownText) {
            countDownText.text = _text;
        }
    }

    private void OnCountDownCancel(object data) {
        countDownText.text = string.Empty;
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
