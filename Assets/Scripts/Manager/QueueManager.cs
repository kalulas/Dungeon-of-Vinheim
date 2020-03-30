using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class QueueManager : Singleton<QueueManager>
{
    private bool waiting = false;
    ExitGames.Client.Photon.Hashtable roomProperties;

    public void SetWaiting(bool value) {
        waiting = value;
    }

    public void QueueAtDirection(Direction direction) {
        roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        int playerInRoom = PhotonNetwork.CurrentRoom.PlayerCount;
        Direction[] dirs = new Direction[] { Direction.Down, Direction.Left, Direction.Right, Direction.Up };

        if (!waiting) {
            waiting = true;
            roomProperties[direction.ToString()] = (int)roomProperties[direction.ToString()] + 1;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            bool waitBalance = false;
            Direction originalDir = Direction.Down;

            foreach (Direction dir in dirs) {
                if ((int)roomProperties[dir.ToString()] == (int)roomProperties[direction.ToString()] && dir != direction) {
                    if ((int)roomProperties[dir.ToString()] == Mathf.Ceil((playerInRoom - 1) / 2)) {
                        originalDir = dir;
                        waitBalance = true;
                        break;
                    }
                }
            }
            // same players at different entrance
            if (waitBalance) {
                GameManager.SendNetworkEvent(EventCode.CancelEnterRoomCountDown, originalDir, raiseEventOptions);
                // cancel count down event with originalDir
            } else if (playerInRoom == 1 || (int)roomProperties[direction.ToString()] == playerInRoom - 1) {
                GameManager.SendNetworkEvent(EventCode.CancelEnterRoomCountDown, direction, raiseEventOptions);
                GameManager.SendNetworkEvent(EventCode.EnterRoom, direction, raiseEventOptions);
                // cancel count down event with direction
                // invoke enter room event with direction
            } else if ((int)roomProperties[direction.ToString()] == Mathf.Ceil((playerInRoom - 1) / 2)) {
                GameManager.SendNetworkEvent(EventCode.StartEnterRoomCountDown, direction, raiseEventOptions);
            }
        } else {
            // logic of entrance's queue : press E second time
            waiting = false;

            roomProperties[direction.ToString()] = (int)roomProperties[direction.ToString()] - 1;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);

            bool waitUnbalance = false;
            Direction anotherDir = Direction.Down;

            foreach (Direction dir in dirs) {
                if ((int)roomProperties[dir.ToString()] > Mathf.Ceil((playerInRoom - 1) / 2)) {
                    waitUnbalance = true;
                    anotherDir = dir;
                    break;
                }
            }
            if (waitUnbalance) {
                GameManager.SendNetworkEvent(EventCode.StartEnterRoomCountDown, anotherDir, raiseEventOptions);
                // invoke count down event with anotherDir
            } else if ((int)roomProperties[direction.ToString()] < Mathf.Ceil((playerInRoom - 1) / 2)) {
                GameManager.SendNetworkEvent(EventCode.CancelEnterRoomCountDown, direction, raiseEventOptions);
                // cancel count down event
            }

        }
    }
    
}
