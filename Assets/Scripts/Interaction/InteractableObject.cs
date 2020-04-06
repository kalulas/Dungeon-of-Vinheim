using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InteractableObject : MonoBehaviour
{
    public bool defaultDiscription = true;
    public string Discription;

    public bool hasAnimation = false;
    public string animationPlayed;
    protected Animation mAnimation;

    private void Start() {
        if (defaultDiscription) {
            Discription = "PRESS E TO INTERACT WITH " + gameObject.name;
        }
        if(hasAnimation) {
            mAnimation = gameObject.GetComponent<Animation>();
        }
    }

    public void PlayAnimation() {
        if (hasAnimation) {
            mAnimation.Play(animationPlayed);
        }
    }

    public void ResetAnimation() {
        AnimationState state = mAnimation[animationPlayed];
        state.time = 0;
        mAnimation.Sample();
        state.enabled = false;
    }

    public virtual void OnAction() {
        Debug.Log("Player Hit Action Button!");
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject == GameManager.localPlayer) {
            ActionManager.Instance.AddIObject(this);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.gameObject == GameManager.localPlayer) {
            ActionManager.Instance.RemoveIObject(this); 
        }
    }
}
