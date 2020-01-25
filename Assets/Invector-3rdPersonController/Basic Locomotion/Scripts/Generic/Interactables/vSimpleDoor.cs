using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Invector
{
    [vClassHeader("Simple Door", openClose = false)]
    public class vSimpleDoor : vMonoBehaviour
    {
        public Transform pivot;
        public bool autoOpen = true;
        public bool autoClose = true;
        public float angleOfOpen = 90f;
        public float angleToInvert = 30f;
        public float speedClose = 2f;
        public float speedOpen = 2f;
        [Tooltip("Used when autoOpenClose is checked")]
        public float timeToClose = 1f;
        [Tooltip("Used when autoOpenClose is checked")]
        public List<string> tagsToOpen = new List<string>() { "Player" };
        [HideInInspector]
        public bool isOpen;
        [HideInInspector]
        public bool isInTransition;
        private Vector3 currentAngle;
        private float forwardDotVelocity;
        private bool invertAngle;
        private bool canOpen;
        public bool stop;
        public NavMeshObstacle navMeshObstacle;
        public UnityEvent onOpen, onClose;

        void Start()
        {
            if (!pivot) enabled = false;
            //  navMeshObstacle = GetComponentInChildren<NavMeshObstacle>();
            if (navMeshObstacle)
            {
                //navMeshObstacle.enabled = false;
                navMeshObstacle.carving = true;
            }
        }

        void OnDrawGizmos()
        {
            if (pivot)
            {
                Gizmos.DrawSphere(transform.position, 0.1f);
                Gizmos.DrawLine(transform.position, pivot.position);
                Gizmos.DrawSphere(pivot.position, 0.1f);
            }
        }

        public void SetAutoOpen(bool value)
        {
            autoOpen = value;
        }

        public void SetAutoClose(bool value)
        {
            autoClose = value;
        }

        public void Open()
        {
            if (!isOpen)
                StartCoroutine(_Open());
        }

        public void Close()
        {
            if (isOpen)
                StartCoroutine(_Close());
        }

        IEnumerator _Open()
        {
            isInTransition = true;
            if (navMeshObstacle)
                navMeshObstacle.enabled = false;
            while (currentAngle.y != (invertAngle ? -angleOfOpen : angleOfOpen))
            {
                yield return new WaitForEndOfFrame();

                if (invertAngle)
                {
                    currentAngle.y -= (100 * speedOpen) * Time.deltaTime;
                    currentAngle.y = Mathf.Clamp(currentAngle.y, -angleOfOpen, 0);
                }
                else
                {
                    currentAngle.y += (100 * speedOpen) * Time.deltaTime;
                    currentAngle.y = Mathf.Clamp(currentAngle.y, 0, angleOfOpen);
                }
                pivot.localEulerAngles = currentAngle;

            }
            isInTransition = false;
            onOpen.Invoke();
            isOpen = true;
        }

        IEnumerator _Close()
        {
            yield return new WaitForSeconds(timeToClose);
            isInTransition = true;
            while (currentAngle.y != 0)
            {
                yield return new WaitForEndOfFrame();
                if (stop)
                    break;
                if (invertAngle)
                {
                    currentAngle.y += (100 * speedClose) * Time.deltaTime;
                    currentAngle.y = Mathf.Clamp(currentAngle.y, -angleOfOpen, 0);
                }
                else
                {
                    currentAngle.y -= (100 * speedClose) * Time.deltaTime;
                    currentAngle.y = Mathf.Clamp(currentAngle.y, 0, angleOfOpen);
                }
                pivot.localEulerAngles = currentAngle;
            }
            if (!stop)
            {
                isInTransition = false;
            }
            stop = false;
            onClose.Invoke();
            isOpen = false;
            if (navMeshObstacle)
                navMeshObstacle.enabled = true;
        }

        void OnTriggerStay(Collider collider)
        {
            if (autoOpen && !isOpen && tagsToOpen.Contains(collider.tag))
            {
                forwardDotVelocity = Mathf.Abs(Vector3.Angle(transform.forward, collider.transform.position - transform.position));
                if (forwardDotVelocity < 60.0f)
                {
                    if (!isInTransition || (currentAngle.y > -angleToInvert && currentAngle.y < angleToInvert))
                        invertAngle = false;
                    canOpen = true;
                }
                else if (forwardDotVelocity >= 60.0f && forwardDotVelocity < 120f)
                {
                    canOpen = false;
                }
                else
                {
                    if (!isInTransition || (currentAngle.y > -angleToInvert && currentAngle.y < angleToInvert))
                        invertAngle = true;
                    canOpen = true;
                }

                if (canOpen && !isOpen)
                {
                    StartCoroutine(_Open());
                }
            }
            else if (isInTransition && isOpen && tagsToOpen.Contains(collider.tag))
            {
                stop = true;
                isOpen = false;
            }
        }

        void OnTriggerExit(Collider collider)
        {
            if (autoClose && isOpen && tagsToOpen.Contains(collider.tag))
            {
                StartCoroutine(_Close());
            }
        }
    }
}