using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public enum InstantiateType {
    OriginalEnemy,
    NewEnemy,
    NewObstacle,
}

public class InstantiateHelper : MonoBehaviour, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info){
        gameObject.SetActive(false);
        object[] data = info.photonView.InstantiationData;
        InstantiateType type = (InstantiateType)data[0];
        int roomNumber = (int)data[1];
        switch (type) {
            case InstantiateType.NewEnemy:
                // roomIdx
                RoomManager.Instance.AddEnemy(gameObject, roomNumber);
                break;
            case InstantiateType.NewObstacle:
                RoomManager.Instance.AddObstacle(gameObject, roomNumber);
                break;
            default:
                break;
        }
    }
}
