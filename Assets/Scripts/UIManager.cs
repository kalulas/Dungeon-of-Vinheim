using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Invector.vCharacterController;
using DungeonOfVinheim;

using Photon.Pun;

public class SetActiveEvent : UnityEvent<bool>{}
public class SetContentEvent : UnityEvent<string, bool>{}
public class UnityEventInt : UnityEvent<int>{}

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public static SetActiveEvent setActionTextActiveEvent = new SetActiveEvent();
    public static SetContentEvent setActionTextContentEvent = new SetContentEvent();
    public static UnityEventInt roomMovedEvent = new UnityEventInt();
    public Button exitGameButton;
    public Button minimapButton;
    public Button[] minimapDirButtons;
    public GameObject mainMenu;
    public GameObject minimap;
    public GameObject minimapMenu;
    public GameObject actionText;
    public Text countDownText;

    private Dictionary<RoomType, Sprite> mapSprite = new Dictionary<RoomType, Sprite>();
    private GameObject[] mapGrid;
    private Button lastMinimapDirBtn;
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

    void Start(){
        exitGameButton.onClick.AddListener(delegate (){
            OnClick(exitGameButton.gameObject);
        });
        minimapButton.onClick.AddListener(delegate (){
            OnClick(minimapButton.gameObject);
        });
        // DrawMinimap();
        // use GUI to show information and ask for permission
        // information depens on event's message
        setActionTextActiveEvent.AddListener(delegate (bool value)
        {
            SetActionTextActive(value);
        });
        setActionTextContentEvent.AddListener(delegate (string content, bool value)
        {
            ChangeActionText(content, value);
        });
        minimapDirButtons[0].onClick.AddListener(() => { moveMinimapFrame(0); });
        minimapDirButtons[1].onClick.AddListener(() => { moveMinimapFrame(1); });
        minimapDirButtons[2].onClick.AddListener(() => { moveMinimapFrame(2); });
        minimapDirButtons[3].onClick.AddListener(() => { moveMinimapFrame(3); });
    }

    void OnClick(GameObject go)
    {
        if (go == exitGameButton.gameObject)
        {
            // TODO: PERMISSION: can add listener to CONFIRM button and invoke later
            PhotonNetwork.LeaveRoom();
        }
        else if(go == minimapButton.gameObject){
            HideAndActive(minimapMenu);
        }
        else if(go.name.StartsWith("room")){
            Debug.Log("room clicked" + go.name);
            selectRoom = int.Parse(go.name.Substring("room".Length));
            SiblingCheck(selectRoom);
        }
    }

    public void moveMinimapFrame(int dir){
        if(selectRoom == -1) return;
        int pace = GameManager.instance.GetPace(selectRoom, (Direction)dir);
        Debug.Log(pace);
        Sprite save = mapGrid[selectRoom + pace].GetComponent<Image>().sprite;
        mapGrid[selectRoom + pace].GetComponent<Image>().sprite = mapGrid[selectRoom].GetComponent<Image>().sprite;
        mapGrid[selectRoom].GetComponent<Image>().sprite = save;
        roomMovedEvent.Invoke(selectRoom);
        selectRoom = selectRoom + pace;
        SiblingCheck(selectRoom);
    }

    public void SetCountDownText(string text){
        if(countDownText) countDownText.text = text;
    }

    public void HideAndActive(GameObject menu){
        if(menuStack.Count != 0) menuStack.Peek().SetActive(false);
        menuStack.Push(menu);
        menu.SetActive(true);
    }

    // triggered on Room Icon Select
    public void SiblingCheck (int roomNumber) {
        int mapSize = GameManager.instance.mapSize;
        int emptyRoomNumber = GameManager.instance.emptyRoomNumber;
        lastMinimapDirBtn?.gameObject.SetActive(false);
        // can be only one direction available
        // TODO: turn gray rather then set active if possible
        if (roomNumber - mapSize == emptyRoomNumber) {
            minimapDirButtons[0].gameObject.SetActive(true);
            lastMinimapDirBtn = minimapDirButtons[0];
        } else if (roomNumber % mapSize != 0 && roomNumber - 1 == emptyRoomNumber) {
            minimapDirButtons[1].gameObject.SetActive(true);
            lastMinimapDirBtn = minimapDirButtons[1];
        } else if (roomNumber % mapSize != mapSize - 1 && roomNumber + 1 == emptyRoomNumber) {
            minimapDirButtons[2].gameObject.SetActive(true);
            lastMinimapDirBtn = minimapDirButtons[2];
        } else if (roomNumber + mapSize == emptyRoomNumber) {
            minimapDirButtons[3].gameObject.SetActive(true);
            lastMinimapDirBtn = minimapDirButtons[3];
        } else {
            lastMinimapDirBtn = null;
        }
    }

    /// <summary>
    /// Draw map of the dungeon on GUI minimap
    /// </summary>
    public void BuildMinimap(){
        int mapSize = GameManager.instance.mapSize;
        mapGrid = new GameObject[mapSize * mapSize];
        RectTransform minimapRT = minimap.GetComponent<RectTransform>();
        float width = minimapRT.rect.width / mapSize;
        float height = minimapRT.rect.height / mapSize;
        float posX = - minimapRT.rect.width / 2 + width / 2;
        float posY = - minimapRT.rect.width / 2 + width / 2;
        // Debug.LogFormat("width:{0}, height:{1}, posX:{2}, posY:{3}", width, height, posX, posY);
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                int roomIdx = mapSize * i + j;
                GameObject room = new GameObject("room" + roomIdx);
                room.transform.SetParent(minimapRT);
                room.transform.localScale = Vector3.one * 0.7f;

                RectTransform rt = room.AddComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(posX + width * j, posY + height * i);
                rt.sizeDelta = new Vector2(width, height);

                room.AddComponent<CanvasRenderer>();
                Image img = room.AddComponent<Image>();
                // img.sprite = Resources.Load<Sprite>("Icons/frame");
                img.sprite = mapSprite[GameManager.instance.GetRoomTypeByIndex(roomIdx)];
                
                UGUIEventListener.Get(room).onClick = OnClick;
                mapGrid[roomIdx] = room;
            }
        }
    }

    public void SetActionTextActive(bool value){
        actionText.SetActive(value);
    }
    
    /// <summary>
    /// <para name="value">value: Used to decide whether to add "PRESS E TO" prefix or not </para>
    /// </summary>
    public void ChangeActionText(string message, bool value){
        string prefix = value ? "PRESS E TO " : "";
        actionText.GetComponent<Text>().text = prefix + message;
    }

    void Update()
    {
        if(Input.GetButtonDown("Escape")){
            // Debug.Log("MUL: Escape pressed!");
            if (menuStack.Count == 0) {
                HideAndActive(mainMenu);
                // unlock cursor from the centor of screen, show cursor and lock all input(basic and melee)
                GameManager.localPlayerInstance.GetComponent<vThirdPersonInput>().LockCursor(true);
                GameManager.localPlayerInstance.GetComponent<vThirdPersonInput>().ShowCursor(true);
                GameManager.localPlayerInstance.GetComponent<vThirdPersonInput>().SetLockAllInput(true);
                GameManager.localPlayerInstance.GetComponent<vThirdPersonInput>().SetLockCameraInput(true);
            }
            else
            {
                menuStack.Peek().SetActive(false);
                menuStack.Pop();
                if (menuStack.Count != 0) menuStack.Peek().SetActive(true);
                else
                {
                    // lock cursor again, hide cursor and unlock all input(basic and melee)
                    GameManager.localPlayerInstance.GetComponent<vThirdPersonInput>().LockCursor(false);
                    GameManager.localPlayerInstance.GetComponent<vThirdPersonInput>().ShowCursor(false);
                    GameManager.localPlayerInstance.GetComponent<vThirdPersonInput>().SetLockAllInput(false);
                    GameManager.localPlayerInstance.GetComponent<vThirdPersonInput>().SetLockCameraInput(false);
                }
            }
        }    
    }
}
