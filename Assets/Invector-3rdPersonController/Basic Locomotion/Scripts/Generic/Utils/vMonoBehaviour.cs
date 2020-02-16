using UnityEngine;

using Photon.Pun;

namespace Invector
{
    // EDIT FROM MonoBehaviour to MonoBeahaviourPun
    public  class vMonoBehaviour : MonoBehaviourPunCallbacks
    {
        [SerializeField, HideInInspector]
        private bool openCloseEvents ;
        [SerializeField, HideInInspector]
        private bool openCloseWindow;
        [SerializeField, HideInInspector]       
        private int selectedToolbar;
    }  
}
