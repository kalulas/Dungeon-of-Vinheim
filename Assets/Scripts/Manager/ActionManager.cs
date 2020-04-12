using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ActionManager : SingletonMonoBehaviour<ActionManager> 
{
    //private Queue<UnityAction> ActionList = new Queue<UnityAction>();
    private Queue<InteractableObject> IObjectList = new Queue<InteractableObject>();

    protected override void Init() {
        MessageCenter.Instance.AddEventListener(GLEventCode.EndRoomTransition, EmptyIObjectList);
    }

    private void EmptyIObjectList(object data) {
        IObjectList.Clear();
    }

    private void DisplayDiscription() {
        string message = string.Empty;
        if (IObjectList.Count != 0) {
            InteractableObject IObject = IObjectList.Peek();
            if (!IObject.duringAction) {
                message = IObject.Discription;
            }
        }
        MessageCenter.Instance.PostGLEvent(GLEventCode.DisplayInteractable, message);
    }

    public void AddIObject(InteractableObject IObject){
        if (!IObjectList.Contains(IObject)) {
            IObjectList.Enqueue(IObject);
            Debug.Log("AddAction, Current Queue: " + IObjectList.Count);
        } else {
            //Debug.Log("Action already in ActionList!");
        }
        
    }

    public void RemoveIObject(InteractableObject IObject) {
        if (IObjectList.Contains(IObject)) {
            Queue<InteractableObject> tmpActionList = new Queue<InteractableObject>();
            while (IObjectList.Count != 0) {
                InteractableObject _action = IObjectList.Dequeue();
                if(_action != IObject) {
                    tmpActionList.Enqueue(_action);
                }
            }

            IObjectList = new Queue<InteractableObject>(tmpActionList);
            Debug.Log("RemoveAction, Current Queue: " + IObjectList.Count);
        } else {
            //Debug.LogFormat("Action {0} not found in ActionList", IObject);
        }
    }

    private void Update() {
        DisplayDiscription();
        if (Input.GetButtonDown("A") && IObjectList.Count != 0) {
            InteractableObject IObject = IObjectList.Peek();
            if (IObject.onlyOnce) {
                IObjectList.Dequeue();
            } 
            if (IObject.gameObject.activeSelf && !IObject.duringAction) {
                IObject.OnAction();
            }
        }
    }

}
