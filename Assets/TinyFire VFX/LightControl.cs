using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightControl : MonoBehaviour {

    float nRand = 0;
	
	void Update ()
    {
        nRand = Random.Range(1f, 1.5f);
        this.transform.GetComponent<Light>().intensity = nRand;
	}
}
