using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public enum RoomPropType {
    GridMap,
    EnemyList,
    ObstaclePath,
    ObstaclePosition,
    WaitDown,
    WaitLeft,
    WaitRight,
    WaitUp,
}

public class RoomPropManager : SingletonMonoBehaviour<RoomPropManager> {
    private Hashtable roomProperties;
    private Dictionary<RoomPropType, string> PropKeys = new Dictionary<RoomPropType, string>() {
        {RoomPropType.GridMap, "gm" },
        {RoomPropType.EnemyList, "el" },
        {RoomPropType.ObstaclePath, "op" },
        {RoomPropType.ObstaclePosition, "op_" },
        {RoomPropType.WaitDown, "wd" },
        {RoomPropType.WaitLeft, "wl" },
        {RoomPropType.WaitRight, "wr" },
        {RoomPropType.WaitUp, "wu" },
    };

    protected override void Init() {
        base.Init();
        roomProperties = new Hashtable() {
            {PropKeys[RoomPropType.WaitDown], 0 },
            {PropKeys[RoomPropType.WaitUp], 0 },
            {PropKeys[RoomPropType.WaitLeft], 0 },
            {PropKeys[RoomPropType.WaitRight], 0 },
        };
        UpLoad();
    }

    private void Download() {
        roomProperties = PhotonNetwork.CurrentRoom.CustomProperties;
    }

    public object GetProp(RoomPropType type) {
        Download();
        if (roomProperties.ContainsKey(PropKeys[type])) {
            return roomProperties[PropKeys[type]];
        } else {
            Debug.LogErrorFormat("Key{0} not found in room properties", PropKeys[type]);
            return null;
        }
    }

    public object GetProp(RoomPropType type, int roomNumber) {
        Download();
        string key = PropKeys[type] + roomNumber.ToString();
        if (roomProperties.ContainsKey(key)) {
            return roomProperties[key];
        } else {
            Debug.LogErrorFormat("Key{0} not found in room properties", key);
            return null;
        }
    }

    public void SetProp(RoomPropType type, object content) {
        roomProperties[PropKeys[type]] = content;
        UpLoad();
    }

    public void SetProp(RoomPropType type, int roomNumber, object content) {
        roomProperties[PropKeys[type] + roomNumber.ToString()] = content;
        UpLoad();
    }

    public void ResetQueue() {
        roomProperties[PropKeys[RoomPropType.WaitDown]] = 0;
        roomProperties[PropKeys[RoomPropType.WaitUp]] = 0;
        roomProperties[PropKeys[RoomPropType.WaitLeft]] = 0;
        roomProperties[PropKeys[RoomPropType.WaitRight]] = 0;
        UpLoad();
    }
    
    public void UpLoad() {
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
    }
}
