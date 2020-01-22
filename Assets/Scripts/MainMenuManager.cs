using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    public Button exitGameButton;
    public Button minimapButton;
    public GameObject minimap;

    void Awake(){
        exitGameButton.onClick.AddListener(delegate (){
            OnClick(exitGameButton.gameObject);
        });
        minimapButton.onClick.AddListener(delegate (){
            OnClick(minimapButton.gameObject);
        });
    }

    // Update is called once per frame
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
            minimap.SetActive(true);
        }
    }
}
