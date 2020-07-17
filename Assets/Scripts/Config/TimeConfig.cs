using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TimeConfig : ScriptableObject {
    public float EntranceQueueWait;
    public float EntranceAnimation;
    public float GameInternal;
    public float LoadSceneWait;
}
