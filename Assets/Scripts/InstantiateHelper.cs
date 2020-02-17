using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class InstantiateHelper : MonoBehaviour, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info){
        this.gameObject.SetActive(false);
    }
}
