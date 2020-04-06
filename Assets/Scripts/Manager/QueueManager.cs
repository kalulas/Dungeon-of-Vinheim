using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class QueueManager : SingletonMonoBehaviour<QueueManager>
{
    private bool waiting = false;
    private int[] queue = new int[4] { 0, 0, 0, 0 };

    protected override void Init() {
        MessageCenter.Instance.AddEventListener(GLEventCode.EndRoomTransition, Reset);
    }

    private void UpdateQueue() {
        queue[(int)Direction.Down] = (int)RoomPropManager.Instance.GetProp(RoomPropType.WaitDown);
        queue[(int)Direction.Left] = (int)RoomPropManager.Instance.GetProp(RoomPropType.WaitLeft);
        queue[(int)Direction.Right] = (int)RoomPropManager.Instance.GetProp(RoomPropType.WaitRight);
        queue[(int)Direction.Up] = (int)RoomPropManager.Instance.GetProp(RoomPropType.WaitUp);
    }

    private bool CheckBalance(Direction newDir, out Direction original) {
        for (int i = 0; i < queue.Length; i++) {
            if(i != (int)newDir && queue[i] == queue[(int)newDir]) {
                original = (Direction)i;
                return true;
            }
        }
        original = Direction.Down;
        return false;
    }

    private bool FindAnotherDir(Direction quitDir, out Direction another, int playerCount) {
        float thres = Mathf.Ceil((playerCount - 1) / 2);
        for (int i = 0; i < queue.Length; i++) {
            if (queue[i] > thres) {
                another = (Direction)i;
                return true;
            }
        }
        another = Direction.Down;
        return false;
    }

    public void Reset(object data) {
        if (PhotonNetwork.IsMasterClient) {
            RoomPropManager.Instance.ResetQueue();
        }
        waiting = false;
        for (int i = 0; i < queue.Length; i++) {
            queue[i] = 0;
        }
    }

    public void QueueAtDirection(Direction direction) {
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        int playerInRoom = PhotonNetwork.CurrentRoom.PlayerCount;

        UpdateQueue();

        if (!waiting) {

            waiting = true;
            queue[(int)direction]++;
            RoomPropManager.Instance.SetProp((RoomPropType)direction + 4, queue[(int)direction]);

            // same players at different entrance
            Direction originalDir = Direction.Down;
            if (CheckBalance(direction, out originalDir)) {
                MessageCenter.Instance.PostNetEvent(NetEventCode.CancelEnterRoomCountDown, originalDir, raiseEventOptions);
                // cancel count down event with originalDir
            } else if (playerInRoom == 1 || queue[(int)direction] == playerInRoom - 1) {
                MessageCenter.Instance.PostNetEvent(NetEventCode.CancelEnterRoomCountDown, direction, raiseEventOptions);
                MessageCenter.Instance.PostNetEvent(NetEventCode.EnterRoom, direction, raiseEventOptions);
                // cancel count down event with direction
                // invoke enter room event with direction
            } else if (queue[(int)direction] == Mathf.Ceil((playerInRoom - 1) / 2)) {
                MessageCenter.Instance.PostNetEvent(NetEventCode.StartEnterRoomCountDown, direction, raiseEventOptions);
            }
        } else {
            // logic of entrance's queue : press E second time
            waiting = false;

            if(queue[(int)direction] == 0) {
                Debug.LogError("Inconsistent! No player at Direction " + direction);
            } else {
                queue[(int)direction]--;
            }
            RoomPropManager.Instance.SetProp((RoomPropType)direction + 4, queue[(int)direction]);
            Direction anotherDir = Direction.Down;

            if (FindAnotherDir(direction, out anotherDir, playerInRoom)) {
                MessageCenter.Instance.PostNetEvent(NetEventCode.StartEnterRoomCountDown, anotherDir, raiseEventOptions);
                // invoke count down event with anotherDir
            } else if (queue[(int)direction] < Mathf.Ceil((playerInRoom - 1) / 2)) {
                MessageCenter.Instance.PostNetEvent(NetEventCode.CancelEnterRoomCountDown, direction, raiseEventOptions);
                // cancel count down event
            }

        }
    }
    
}
