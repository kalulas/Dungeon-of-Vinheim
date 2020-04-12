using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBox : InteractableObject
{
    public string appendix;

    public override void OnAction() {
        base.OnAction();
        gameObject.transform.LookAt(GameManager.localPlayer.transform);
        EndAction();
    }
}
