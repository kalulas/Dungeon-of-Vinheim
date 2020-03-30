using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class LoadObject {
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public GameObject m_object;
}

[Serializable]
public class Range2DRect {
    public Vector2 Center;
    public Vector2 Size;
    public Vector3[] GetBorder() {
        Vector3[] border = new Vector3[4] {
                new Vector3(Center.x - Size.x/2, 0, Center.y + Size.y/2),
                new Vector3(Center.x + Size.x/2, 0, Center.y + Size.y/2),
                new Vector3(Center.x + Size.x/2, 0, Center.y - Size.y/2),
                new Vector3(Center.x - Size.x/2, 0, Center.y - Size.y/2),
        };
        return border;
    }
}

[CreateAssetMenu]
public class LoadPositionConfig : ScriptableObject
{
    public bool range;
    public int size;
    public Range2DRect innerBound;
    public Range2DRect outerBound;
    public List<LoadObject> loads;
}
