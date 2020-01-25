using UnityEngine;
using Invector;

[vClassHeader(" Weapon Constrain ", "Weapon Constrain Helper: call true OnEquipe and false OnDrop by CollectableStandalone events to avoid handler movement on 2018.x", iconName = "meleeIcon")]
public class vWeaponConstrain : vMonoBehaviour
{
    Rigidbody m_Rigidbody;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    // Call OnEquipe (true) / OnDrop (false)
    public void Inv_Weapon_FreezeAll(bool status)
    {        
        if (status)
        {
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
        else
        {
            m_Rigidbody.constraints = RigidbodyConstraints.None;
        }        
    }
}