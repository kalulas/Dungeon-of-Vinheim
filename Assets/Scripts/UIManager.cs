using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Invector.vCharacterController;

public class SetActiveEvent : UnityEvent<bool>{}
public class SetContentEvent : UnityEvent<string, bool>{}

public class UIManager : MonoBehaviour
{
    public static SetActiveEvent setActionTextActiveEvent = new SetActiveEvent();
    public static SetContentEvent setActionTextContentEvent = new SetContentEvent();
    public Button exitGameButton;
    public Button minimapButton;
    public GameObject mainMenu;
    public GameObject minimap;
    public GameObject minimapMenu;
    public GameObject actionText;

    private Stack<GameObject> menuStack = new Stack<GameObject>();

    void Start(){
        exitGameButton.onClick.AddListener(delegate (){
            OnClick(exitGameButton.gameObject);
        });
        minimapButton.onClick.AddListener(delegate (){
            OnClick(minimapButton.gameObject);
        });
        DrawMinimap();
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
    }

    void OnClick(GameObject go)
    {
        if (go == exitGameButton.gameObject)
        {
            // TODO: PERMISSION: can add listener to CONFIRM button and invoke later
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        else if(go == minimapButton.gameObject){
            HideAndActive(minimapMenu);
        }
    }

    public void HideAndActive(GameObject menu){
        if(menuStack.Count != 0) menuStack.Peek().SetActive(false);
        menuStack.Push(menu);
        menu.SetActive(true);
    }

    /// <summary>
    /// Draw map of the dungeon on GUI minimap
    /// </summary>
    public void DrawMinimap(){
        int mapSize = GameManager.instance.GetMapSize();
        RectTransform minimapRT = minimap.GetComponent<RectTransform>();
        float width = minimapRT.rect.width / mapSize;
        float height = minimapRT.rect.height / mapSize;
        float posX = - minimapRT.rect.width / 2 + width / 2;
        float posY = - minimapRT.rect.width / 2 + width / 2;
        Debug.LogFormat("width:{0}, height:{1}, posX:{2}, posY:{3}", width, height, posX, posY);
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
                img.sprite = Resources.Load<Sprite>("Icons/frame");
                switch (GameManager.instance.GetRoomTypeByIndex(roomIdx))
                {
                    case RoomType.BattleRoom: img.sprite = Resources.Load<Sprite>("Icons/sword");break;
                    case RoomType.BossRoom: img.sprite = Resources.Load<Sprite>("Icons/scroll");break;
                    case RoomType.RewardRoom: img.sprite = Resources.Load<Sprite>("Icons/coins");break;
                    case RoomType.StartingRoom: img.sprite = Resources.Load<Sprite>("Icons/helmets");break;
                    case RoomType.EmptyRoom: img.sprite = Resources.Load<Sprite>("Icons/frame");break;
                    default:break;
                }
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
            if (menuStack.Count == 0) {
                HideAndActive(mainMenu);
                // unlock cursor from the centor of screen, show cursor and lock all input(basic and melee)
                GameObject.FindGameObjectWithTag("Player").SendMessage("LockCursor", true);
                // ↓ same idea different method
                // GameObject.FindGameObjectWithTag("Player").GetComponent<vThirdPersonInput>().SetLockBasicInput(true);
                GameObject.FindGameObjectWithTag("Player").SendMessage("ShowCursor", true);
                GameObject.FindGameObjectWithTag("Player").SendMessage("SetLockAllInput", true);
            }
            else
            {
                menuStack.Peek().SetActive(false);
                menuStack.Pop();
                if (menuStack.Count != 0) menuStack.Peek().SetActive(true);
                else
                {
                    // lock cursor again, hide cursor and unlock all input(basic and melee)
                    GameObject.FindGameObjectWithTag("Player").SendMessage("LockCursor", false);
                    GameObject.FindGameObjectWithTag("Player").SendMessage("ShowCursor", false);
                    GameObject.FindGameObjectWithTag("Player").SendMessage("SetLockAllInput", false);
                }
            }
        }    
    }
}
