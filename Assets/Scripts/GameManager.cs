using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 单例脚本管理地图信息并进行关卡切换
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    static GameManager(){
        GameObject gm = new GameObject("#GameManager#");
        DontDestroyOnLoad(gm);
        // 这样GameManager的脚本就被绑定在GameObject上，可以在生命周期中进行自定义操作（Start Update）
        // 是否需要依赖于脚本看后续如何编写
        instance = gm.AddComponent<GameManager>();
    }

    public void ShowYourself(){
        Debug.Log("Game Manager exists.");
    }
    // Start is called before the first frame update
    // void Start()
    // {
    //     Debug.Log("instance of 'Game Manager' has been generated.");
    // }

    // Update is called once per frame
    // void Update()
    // {
        
    // }
}
