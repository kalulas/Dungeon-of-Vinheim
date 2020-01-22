using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// might contiains other triggers later on like items or monsters...?
public enum TriggerEventType{
    Entrance,
    NULL
}
// direction that player enters the next room
public enum Direction{
    Down,
    Left,
    Right,
    Up
}

public class TriggerEvent : UnityEvent<Collider>{
    public TriggerEventType type;
    public Direction direction;
}

public class ItemTrigger : MonoBehaviour
{
    // Trigger Event will now bring the information of the entrance
    public TriggerEventType type;
    public Direction direction;
    // NOTE: only have one triggerEventEnter (static)
    public static TriggerEvent triggerEventEnter = new TriggerEvent();
    public static TriggerEvent triggerEventStay = new TriggerEvent();

    // void OnTriggerEnter(Collider collider){
    //     if(type == TriggerEventType.Entrance) triggerEventEnter.direction = direction;
    //     triggerEventEnter.type = type;
    //     triggerEventEnter.Invoke(collider);
    // }

    void OnTriggerStay(Collider collider){
        if(collider.gameObject.tag == "Player"){
            if(type == TriggerEventType.Entrance) triggerEventStay.direction = direction;
            triggerEventStay.type = type;
            triggerEventStay.Invoke(collider);
        }
    }
}
