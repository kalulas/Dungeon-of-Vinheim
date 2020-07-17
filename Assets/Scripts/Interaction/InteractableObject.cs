using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InteractableObject : MonoBehaviour
{
    public bool defaultDiscription = true;
    public string Discription;
    public bool onlyOnce = true; // interact only once while player nearby
    public bool duringAction = false;

    public bool hasAnimation = false;
    public string animationPlayed;

    protected void Start() {
        if (defaultDiscription) {
            Discription = "PRESS E TO INTERACT WITH " + gameObject.name;
        }
        if(hasAnimation) {
            AnimationManager.Instance.RegisterAnimation(gameObject.GetInstanceID(), gameObject.GetComponent<Animation>());
        }
        Init();
    }

    protected virtual void Init() {

    }

    public virtual void OnAction() {
        duringAction = true;
    }

    private void OnDestroy() {
        if (hasAnimation) {
            if(AnimationManager.IsCreate) {
                AnimationManager.Instance.RemoveAnimation(gameObject.GetInstanceID());
            }
        }
    }

    public void EndAction() {
        duringAction = false;
    }

    private void OnDisable() {
        // avoid user action list occupied with invisible actions
        if (ActionManager.IsCreate) {
            ActionManager.Instance.RemoveIObject(this);
        }
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
