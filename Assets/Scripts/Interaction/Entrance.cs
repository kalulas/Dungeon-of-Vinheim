using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction {
    Down,
    Left,
    Right,
    Up,
    Center
}

public class Entrance : InteractableObject
{
    public Direction direction;

    protected override void Init() {
        base.Init();
        MessageCenter.Instance.AddEventListener(GLEventCode.EndRoomTransition, OnEndAction);
        MessageCenter.Instance.AddObserver(NetEventCode.CancelEnterRoomCountDown, OnEndAction);
    }

    public override void OnAction() {
        base.OnAction();
        if (RoomManager.Instance.entranceAvailable) {
            if (!RoomManager.Instance.AvailableAhead(direction)) {
                MessageCenter.Instance.PostGLEvent(GLEventCode.DisplayFadeText, "BLOCKED: NO ROOM AHEAD");
            } else {
                QueueManager.Instance.QueueAtDirection(direction);
            }
        } else {
            MessageCenter.Instance.PostGLEvent(GLEventCode.DisplayFadeText, "BLOCKED: CLOSED BY HOST");
        }
        EndAction();
    }

    private void OnEndAction(object data) {
        if((Direction)data == direction) {
            EndAction();
        }
    }
}
