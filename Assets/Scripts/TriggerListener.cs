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
public enum EnterDirection{
    Down,
    Left,
    Right,
    Up
}

public class TriggerEvent : UnityEvent<Collider>{
    public TriggerEventType type;
    public EnterDirection direction;
}

public class TriggerListener : MonoBehaviour
{
    // Trigger Event will now bring the information of the entrance
    public TriggerEventType type;
    public EnterDirection direction;
    // NOTE: only have one triggerEventEnter (static)
    public static TriggerEvent triggerEventEnter = new TriggerEvent();

    void OnTriggerEnter(Collider collider){
        if(type == TriggerEventType.Entrance) triggerEventEnter.direction = direction;
        triggerEventEnter.type = type;
        triggerEventEnter.Invoke(collider);
    }
}
