using UnityEngine;

namespace Invector.Utils
{    public class vUpdateUIPosition : MonoBehaviour
    {
        public Transform referenceLocalParent;
        public bool updateLocalX, updateLocalY, updateLocalZ;

        public void UpdatePosition(GameObject target)
        {
            SetLocalPosition(target.transform);
        }

        public void UpdatePosition(Collider target)
        {
            SetLocalPosition(target.transform);
        }

        public void UpdatePosition(Transform target)
        {
            SetLocalPosition(target);
        }

        void SetLocalPosition(Transform target)
        {
            var localPosition = referenceLocalParent.InverseTransformPoint(target.position);
            var selfLocalPosition = transform.localPosition;
            if (updateLocalX) selfLocalPosition.x = localPosition.x;
            if (updateLocalY) selfLocalPosition.y = localPosition.y;
            if (updateLocalZ) selfLocalPosition.z = localPosition.z;
            transform.localPosition = selfLocalPosition;
        }
    }
}