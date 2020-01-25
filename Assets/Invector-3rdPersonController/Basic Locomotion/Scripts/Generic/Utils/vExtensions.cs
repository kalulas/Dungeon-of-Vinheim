using System;
using System.Collections.Generic;
using UnityEngine;

namespace Invector
{
    public static class vExtensions
    {
        public static bool IsVectorNaN(this Vector3 vector)
        {
            return float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z);
        }

        public static Vector3[] MakeSmoothCurve(this Vector3[] pts, float smoothFactor = 0.25f)
        {
            smoothFactor = Mathf.Clamp(smoothFactor, 0.1f, 0.9f);
            Vector3[] newPts = new Vector3[(pts.Length - 2) * 2 + 2];
            try
            {
                newPts[0] = pts[0];
                newPts[newPts.Length - 1] = pts[pts.Length - 1];

                int j = 1;
                for (int i = 0; i < pts.Length - 2; i++)
                {
                    newPts[j] = pts[i] + (pts[i + 1] - pts[i]) * (1f - smoothFactor);
                    newPts[j + 1] = pts[i + 1] + (pts[i + 2] - pts[i + 1]) * smoothFactor;
                    j += 2;
                }
            }
            catch
            {
                newPts = pts;
            }
            return newPts;
        }

        public static List<Vector3> MakeSmoothCurve(this List<Vector3> pts, float smoothFactor = 0.25f)
        {
            smoothFactor = Mathf.Clamp(smoothFactor, 0.1f, 0.9f);
            List<Vector3> newPts = new List<Vector3>((pts.Count - 2) * 2 + 2);
            try
            {

                newPts[0] = pts[0];
                newPts[newPts.Count - 1] = pts[pts.Count - 1];

                int j = 1;
                for (int i = 0; i < pts.Count - 2; i++)
                {
                    newPts[j] = pts[i] + (pts[i + 1] - pts[i]) * (1f - smoothFactor);
                    newPts[j + 1] = pts[i + 1] + (pts[i + 2] - pts[i + 1]) * smoothFactor;
                    j += 2;
                }
            }
            catch
            {
                newPts = pts;
            }
            return newPts;
        }

        public static Vector3[] MakeSmoothCurveArray(this List<Vector3> pts, float smoothFactor = 0.25f)
        {
            smoothFactor = Mathf.Clamp(smoothFactor, 0.1f, 0.9f);
            Vector3[] newPts = new Vector3[(pts.Count - 2) * 2 + 2];
            try
            {

                newPts[0] = pts[0];
                newPts[newPts.Length - 1] = pts[pts.Count - 1];

                int j = 1;
                for (int i = 0; i < pts.Count - 2; i++)
                {
                    newPts[j] = pts[i] + (pts[i + 1] - pts[i]) * (1f - smoothFactor);
                    newPts[j + 1] = pts[i + 1] + (pts[i + 2] - pts[i + 1]) * smoothFactor;
                    j += 2;
                }
            }
            catch
            {
                newPts = pts.vToArray();
            }
            return newPts;
        }
        public static void SetLayerRecursively(this GameObject obj, int layer)
        {
            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        public static bool ContainsLayer(this LayerMask layermask, int layer)
        {
            return layermask == (layermask | (1 << layer));
        }

        public static void SetActiveChildren(this GameObject gameObjet, bool value)
        {
            foreach (Transform child in gameObjet.transform)
                child.gameObject.SetActive(value);
        }

        /// <summary>
        /// Check if Transfom is children
        /// </summary>
        /// <param name="me"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool isChild(this Transform me, Transform target)
        {
            if (!target) return false;
            var objName = target.gameObject.name;
            var obj = me.FindChildByNameRecursive(objName);
            if (obj == null) return false;
            else return obj.Equals(target);
        }

        static Transform FindChildByNameRecursive(this Transform me, string name)
        {
            if (me.name == name)
                return me;
            for (int i = 0; i < me.childCount; i++)
            {
                var child = me.GetChild(i);
                var found = child.FindChildByNameRecursive(name);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Normalized the angle. between -180 and 180 degrees
        /// </summary>
        /// <param Name="eulerAngle">Euler angle.</param>
        public static Vector3 NormalizeAngle(this Vector3 eulerAngle)
        {
            var delta = eulerAngle;

            if (delta.x > 180) delta.x -= 360;
            else if (delta.x < -180) delta.x += 360;

            if (delta.y > 180) delta.y -= 360;
            else if (delta.y < -180) delta.y += 360;

            if (delta.z > 180) delta.z -= 360;
            else if (delta.z < -180) delta.z += 360;

            return new Vector3(delta.x, delta.y, delta.z);//round values to angle;
        }

        public static Vector3 Difference(this Vector3 vector, Vector3 otherVector)
        {
            return otherVector - vector;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            do
            {
                if (angle < -360)
                    angle += 360;
                if (angle > 360)
                    angle -= 360;
            } while (angle < -360 || angle > 360);

            return Mathf.Clamp(angle, min, max);
        }

        public static T[] Append<T>(this T[] arrayInitial, T[] arrayToAppend)
        {
            if (arrayToAppend == null)
            {
                throw new ArgumentNullException("The appended object cannot be null");
            }
            if ((arrayInitial is string) || (arrayToAppend is string))
            {
                throw new ArgumentException("The argument must be an enumerable");
            }
            T[] ret = new T[arrayInitial.Length + arrayToAppend.Length];
            arrayInitial.CopyTo(ret, 0);
            arrayToAppend.CopyTo(ret, arrayInitial.Length);

            return ret;
        }

        public static List<T> vCopy<T>(this List<T> list)
        {
            List<T> _list = new List<T>();
            if (list == null || list.Count == 0) return list;
            for (int i = 0; i < list.Count; i++)
            {
                _list.Add(list[i]);
            }
            return _list;
        }

        public static List<T> vToList<T>(this T[] array)
        {
            List<T> list = new List<T>();
            if (array == null || array.Length == 0) return list;
            for (int i = 0; i < array.Length; i++)
            {
                list.Add(array[i]);
            }
            return list;
        }

        public static T[] vToArray<T>(this List<T> list)
        {
            T[] array = new T[list.Count];
            if (list == null || list.Count == 0) return array;
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = list[i];
            }
            return array;
        }

        public static Vector3 BoxSize(this BoxCollider boxCollider)
        {
            var length = boxCollider.transform.lossyScale.x * boxCollider.size.x;
            var width = boxCollider.transform.lossyScale.z * boxCollider.size.z;
            var height = boxCollider.transform.lossyScale.y * boxCollider.size.y;
            return new Vector3(length, height, width);
        }

        public static bool Contains<T>(this Enum value, Enum lookingForFlag) where T : struct
        {
            int intValue = (int)(object)value;
            int intLookingForFlag = (int)(object)lookingForFlag;
            return ((intValue & intLookingForFlag) == intLookingForFlag);
        }
        /// <summary>
        /// Load all <see cref="vCharacterController.vActions.IActionController"/> and derivatives  in character gameObject to register to events <see cref="vCharacterController.vCharacter.onActionEnter"/>,<see cref="vCharacterController.vCharacter.onActionStay"/> and <see cref="vCharacterController.vCharacter.onActionExit"/>.
        /// </summary>
        /// <param name="character">Target <seealso cref="vCharacterController.vCharacter>"/></param>
    }
}
