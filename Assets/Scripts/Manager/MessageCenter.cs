using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public struct GLEventData {
    public GLEventCode code;
    public object data;
}

public delegate void NetEventCallback(object data);
public delegate void GLEventCallback(object data);

public class MessageCenter : SingletonMonoBehaviour<MessageCenter> {
    private Dictionary<NetEventCode, NetEventCallback> NetEventList = new Dictionary<NetEventCode, NetEventCallback>();
    public Queue<EventData> NetEventDataQueue = new Queue<EventData>();

    private Dictionary<GLEventCode, GLEventCallback> GLEventList = new Dictionary<GLEventCode, GLEventCallback>();
    public Queue<GLEventData> GLEventDataQueue = new Queue<GLEventData>();

    public void PostNetEvent(NetEventCode eventCode, object content, RaiseEventOptions raiseEventOptions) {
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent((byte)eventCode, content, raiseEventOptions, sendOptions);
    }

    public void AddObserver(NetEventCode code, NetEventCallback callback) {
        if (NetEventList.ContainsKey(code)) {
            NetEventList[code] += callback;
        } else {
            NetEventList.Add(code, callback);
        }
    }

    public void RemoveObserver(NetEventCode code, NetEventCallback callback) {
        if (NetEventList.ContainsKey(code)) {
            NetEventList[code] -= callback;
            if (NetEventList[code] == null) {
                NetEventList.Remove(code);
            }
        }
    }

    public void AddEventListener(GLEventCode code, GLEventCallback callback) {
        if (GLEventList.ContainsKey(code)) {
            GLEventList[code] += callback;
        } else {
            GLEventList.Add(code, callback);
        }
    }

    public void RemoveEventListener(GLEventCode code, GLEventCallback callback) {
        if (GLEventList.ContainsKey(code)) {
            GLEventList[code] -= callback;
            if(GLEventList[code] == null) {
                GLEventList.Remove(code);
            }
        }
    }

    public void PostGLEvent(GLEventCode code, object data = null) {
        if (GLEventList.ContainsKey(code)) {
            GLEventList[code](data);
        }
    }

    void Update() {

        while(GLEventDataQueue.Count > 0) {
            GLEventData tmpGLeventData = GLEventDataQueue.Dequeue();
            if (GLEventList.ContainsKey(tmpGLeventData.code)) {
                GLEventList[tmpGLeventData.code](tmpGLeventData.data);
            }
        }

        while(NetEventDataQueue.Count > 0) {
            lock(NetEventDataQueue) {
                EventData tmpNetEventData = NetEventDataQueue.Dequeue();
                if (NetEventList.ContainsKey((NetEventCode)tmpNetEventData.Code)) {
                    NetEventList[(NetEventCode)tmpNetEventData.Code](tmpNetEventData.CustomData);
                }
            }
        }
    }

}
