using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Invector.Utils
{
    public class vTargetLookAt : MonoBehaviour
    {
        public Transform target;
        public float smoot;
        // Update is called once per frame
        void Update()
        {
            var dir = target.position - transform.position;
            Quaternion rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, smoot * Time.deltaTime);
        }
    }
}