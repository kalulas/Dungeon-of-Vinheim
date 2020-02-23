using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaitTimeManager
{
    public class TaskBehaviour : MonoBehaviour { }
    private static TaskBehaviour task;

    static IEnumerator NoInternalCoroutine(float time, UnityAction callback){
        yield return new WaitForSeconds(time);
        if(callback != null) callback();
    }

    static IEnumerator InternalCoroutine(bool kickstart, float time, float intervalTime, UnityAction callback){
        yield return new RepeatForSeconds(kickstart, time, intervalTime, callback);
    }

    static WaitTimeManager(){
        GameObject go = new GameObject("#WaitTimeManager#");
        GameObject.DontDestroyOnLoad(go);
        // in order to make gameobject task has life cycle
        task = go.AddComponent<TaskBehaviour>();
    }

    /// <param name="interval">determine whether this is a repeated coroutine</param>
    static public Coroutine CreateCoroutine(bool interval, float time, UnityAction callback, float intervalTime = 1.0f, bool kickstart = false)
    {
        if (!interval) return task.StartCoroutine(NoInternalCoroutine(time, callback));
        else return task.StartCoroutine(InternalCoroutine(kickstart, time, intervalTime, callback));
    }

    static public void CancelCoroutine(ref Coroutine coroutine){
        if(coroutine != null){
            task.StopCoroutine(coroutine);
            coroutine = null;
        }
    }
}

public class RepeatForSeconds : CustomYieldInstruction
{
    private UnityAction intervalCallback;
    private float startTime;
    private float lastTime;
    private float interval;
    private float lastForSeconds;

    public override bool keepWaiting{
        get{
            if(Time.time - startTime >= lastForSeconds) return false;
            else if(Time.time - lastTime >= interval){
                lastTime = Time.time;
                if(intervalCallback != null) intervalCallback();
            }
            return true;
        }
    }

    public RepeatForSeconds(bool kickstart, float time, float interval, UnityAction callback){
        if(kickstart) callback();
        startTime = Time.time;
        lastTime = Time.time;
        this.interval = interval;
        lastForSeconds = time;
        intervalCallback = callback;
    }
}
