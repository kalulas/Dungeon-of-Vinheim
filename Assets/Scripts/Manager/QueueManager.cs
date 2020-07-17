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
        Debug.LogFormat("[QueueManager] current queue: down {0}, left {1}, right {2}, up {3}", queue[0], queue[1], queue[2], queue[3]);
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
        int playerInRoom = PhotonNetwork.CurrentRoom.PlayerCount;
        UpdateQueue();

        if (!waiting) {
            Debug.LogFormat("[QueueManager] enter queue at direction {0}", direction);
            waiting = true;
            queue[(int)direction]++;
            RoomPropManager.Instance.SetProp((RoomPropType)((int)RoomPropType.WaitDown + direction) , queue[(int)direction]);

            // same players at different entrance
            Direction originalDir = Direction.Down;
            if (CheckBalance(direction, out originalDir)) {
                MessageCenter.Instance.PostNetEvent2All(NetEventCode.CancelEnterRoomCountDown, originalDir);
                // cancel count down event with originalDir
            } else if (playerInRoom == 1 || queue[(int)direction] == playerInRoom - 1) {
                MessageCenter.Instance.PostNetEvent2All(NetEventCode.CancelEnterRoomCountDown, direction);
                MessageCenter.Instance.PostNetEvent2All(NetEventCode.EnterRoom, direction);
                // cancel count down event with direction
                // invoke enter room event with direction
            } else if (queue[(int)direction] == Mathf.Ceil((playerInRoom - 1) / 2)) {
                MessageCenter.Instance.PostNetEvent2All(NetEventCode.StartEnterRoomCountDown, direction);
            }
        } else {
            Debug.LogFormat("[QueueManager] exit queue at direction {0}", direction);
            // logic of entrance's queue : press E second time
            waiting = false;

            if(queue[(int)direction] == 0) {
                Debug.LogError("Inconsistent! No player at Direction " + direction);
            } else {
                queue[(int)direction]--;
            }
            RoomPropManager.Instance.SetProp((RoomPropType)((int)RoomPropType.WaitDown + direction), queue[(int)direction]);
            Direction anotherDir = Direction.Down;

            if (FindAnotherDir(direction, out anotherDir, playerInRoom)) {
                MessageCenter.Instance.PostNetEvent2All(NetEventCode.StartEnterRoomCountDown, anotherDir);
                // invoke count down event with anotherDir
            } else if (queue[(int)direction] < Mathf.Ceil((playerInRoom - 1) / 2)) {
                MessageCenter.Instance.PostNetEvent2All(NetEventCode.CancelEnterRoomCountDown, direction);
                // cancel count down event
            }

        }
    }
    
}
