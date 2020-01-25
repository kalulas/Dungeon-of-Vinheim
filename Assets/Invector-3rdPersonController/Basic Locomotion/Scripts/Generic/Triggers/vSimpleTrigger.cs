using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Invector
{
    [RequireComponent(typeof(BoxCollider))]
    [vClassHeader("SimpleTrigger", openClose = false)]
    public class vSimpleTrigger : vMonoBehaviour
    {
        [System.Serializable]
        public class vTriggerEvent : UnityEvent<Collider> { }

        public bool useFilter = true;
        public List<string> tagsToDetect = new List<string>() { "Player" };
        public LayerMask layerToDetect = 0 << 1;
        public vTriggerEvent onTriggerEnter;
        public vTriggerEvent onTriggerExit;

        [HideInInspector]
        public bool inCollision;
        private bool triggerStay;
        private Collider other;

        void OnDrawGizmos()
        {
            Color red = new Color(1, 0, 0, 0.2f);
            Color green = new Color(0, 1, 0, 0.2f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, (transform.lossyScale));
            Gizmos.color = inCollision && Application.isPlaying ? red : green;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }

        void Start()
        {
            inCollision = false;
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!useFilter || tagsToDetect.Contains(other.gameObject.tag) && IsInLayerMask(other.gameObject, layerToDetect) && this.other == null)
            {
                inCollision = true;
                this.other = other;
                onTriggerEnter.Invoke(other);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!useFilter || tagsToDetect.Contains(other.gameObject.tag) && IsInLayerMask(other.gameObject, layerToDetect) && (this.other == null || this.other.gameObject == other.gameObject))
            {
                inCollision = false;
                onTriggerExit.Invoke(other);
                this.other = null;
            }
        }

        bool IsInLayerMask(GameObject obj, LayerMask mask)
        {
            return ((mask.value & (1 << obj.layer)) > 0);
        }
    }
}