using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class EnemiesConfig : ScriptableObject
{
    public List<string> normalPathList;
    public List<string> elitePathList;
    public List<string> bossPathList;
}
