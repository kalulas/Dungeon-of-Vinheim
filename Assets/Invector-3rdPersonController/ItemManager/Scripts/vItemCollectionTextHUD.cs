using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Invector.vItemManager
{
    public class vItemCollectionTextHUD : MonoBehaviour
    {
        public Text Message;
        [HideInInspector]
        public bool inUse = false;

        public void Show(string message, float timeToStay = 1, float timeToFadeOut = 1)
        {
            inUse = true;
            Message.text = message;
            StartCoroutine(Timer(timeToStay, timeToFadeOut));
        }

        IEnumerator Timer(float timeToStay = 1, float timeToFadeOut = 1)
        {
            Message.CrossFadeAlpha(1, 0.5f, false);

            yield return new WaitForSeconds(timeToStay);
            Message.CrossFadeAlpha(0, timeToFadeOut, false);

            yield return new WaitForSeconds(timeToFadeOut + 0.1f);
            Destroy(gameObject);
            inUse = false;
        }

        public void Init()
        {
            Message.text = "";
            Message.CrossFadeAlpha(0, 0, false);
        }
    }
}
