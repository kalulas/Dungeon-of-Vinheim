using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : SingletonMonoBehaviour<AnimationManager>
{
    // gameObjectID -> Animation
    private Dictionary<int, Animation> AnimationDict = new Dictionary<int, Animation>();

    protected override void Init() {
        MessageCenter.Instance.AddEventListener(GLEventCode.PlayAnimation, PlayAnimation);
        MessageCenter.Instance.AddEventListener(GLEventCode.ResetAnimation, ResetAnimation);
    }

    public void RegisterAnimation(int instanceID, Animation animation) {
        if (!AnimationDict.ContainsKey(instanceID)) {
            AnimationDict.Add(instanceID, animation);
        } else {
            Debug.LogWarning("AnimationManager Register(): Duplicate Keys");
        }
    }

    public void RemoveAnimation(int instanceID) {
        if(!AnimationDict.ContainsKey(instanceID)) {
            AnimationDict.Remove(instanceID);
        } else {
            Debug.LogWarning("AnimationManager Remove(): Key Not Existed.");
        }
    }

    private void PlayAnimation(object data) {
        object[] _data = (object[])data;
        int instanceID = (int)_data[0];
        string played = (string)_data[1];
        Debug.LogFormat("AnimationManager PlayAnimation():{0}:{1}", instanceID, played);
        if (AnimationDict.ContainsKey(instanceID)) {
            AnimationDict[instanceID].Play(played);
        }
    }

    private void ResetAnimation(object data) {
        object[] _data = (object[])data;
        int instanceID = (int)_data[0];
        string played = (string)_data[1];
        Debug.LogFormat("AnimationManager ResetAnimation():{0}:{1}", instanceID, played);
        if (AnimationDict.ContainsKey(instanceID)) {
            Animation animation = AnimationDict[instanceID];
            AnimationState state = animation[played];
            if(state != null) {
                state.time = 0;
                animation.Sample();
                state.enabled = false;
            }
        }
    }
}
