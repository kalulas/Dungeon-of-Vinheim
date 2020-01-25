using System.Collections;
using UnityEngine;

namespace Invector.Utils
{
    [vClassHeader("Events With Delay")]
    public class vEventWithDelay : vMonoBehaviour
    {
        public bool triggerOnStart;
        [vHideInInspector("triggerOnStart")]
        public bool all;
        [vHideInInspector("triggerOnStart")]
        public int eventIndex;
        private void Start()
        {
            if(triggerOnStart)
            {
                if (all) DoEvents();
                else DoEvent(eventIndex);
            }
        }
        [SerializeField] private vEventWithDelayObject[] events = new vEventWithDelayObject[0];
        public void DoEvents()
        {
            for (int i = 0; i < events.Length; i++)
                StartCoroutine(DoEventWithDelay(events[i]));
        }

        public void DoEvent(int index)
        {

            if (index < events.Length && events.Length > 0) StartCoroutine(DoEventWithDelay(events[index]));
        }
        public void DoEvent(string name)
        {
            vEventWithDelayObject _e = System.Array.Find(events, e => e.name.Equals(name));
            if(_e!=null) StartCoroutine(DoEventWithDelay(_e));
        }
        IEnumerator DoEventWithDelay(vEventWithDelayObject _event)
        {
            yield return new WaitForSeconds(_event.delay);
            _event.onDoEvent.Invoke();
        }

        [System.Serializable]
        public class vEventWithDelayObject
        {
            public string name = "EventName";
            public float delay;          
            public UnityEngine.Events.UnityEvent onDoEvent;
        }
    }
}