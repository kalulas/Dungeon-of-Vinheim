using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Invector.vItemManager
{
    public class vItemCollectionDisplay : MonoBehaviour
    {
        private static vItemCollectionDisplay instance;
        public static vItemCollectionDisplay Instance
        {
            get
            {
                if (instance == null) { instance = GameObject.FindObjectOfType<vItemCollectionDisplay>(); }
                return vItemCollectionDisplay.instance;
            }
        }       
       
        public GameObject HeadsUpText;
        public Transform Contenet;            

        public void FadeText(string message, float timeToStay, float timeToFadeOut)
        {            
            var itemObj = Instantiate(HeadsUpText) as GameObject;
            itemObj.transform.SetParent(Contenet, false);

            vItemCollectionTextHUD textHud = itemObj.GetComponent<vItemCollectionTextHUD>();
            if (!textHud.inUse)
            {
                textHud.transform.SetAsFirstSibling();
                textHud.Init();
                textHud.Show(message, timeToStay, timeToFadeOut);                    
            }            
        }      
    }
}

