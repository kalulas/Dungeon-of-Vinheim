using Invector.vMelee;
using UnityEngine;
namespace Invector.vItemManager
{
    [vClassHeader("Melee Equipment",openClose = false, useHelpBox = true, helpBoxText = "This is a link for ItemManager")]
    public class vMeleeEquipment : vMonoBehaviour, vIEquipment
    {
        vMeleeWeapon _weapon;
        bool withoutWeapon;
        
        public vMeleeWeapon weapon
        {
            get
            {
                if (!_weapon && !withoutWeapon)
                {
                    _weapon = GetComponent<vMeleeWeapon>();
                    if (!_weapon) withoutWeapon = true;
                }

                return _weapon;
            }
        }

        public vItem referenceItem
        {
            get;protected set;
        }

        public bool isEquiped
        {
            get;protected set;
           
        }

        public EquipPoint equipPoint { get ; set; }

        public void OnEquip(vItem item)
        {
            referenceItem = item;
            isEquiped = true;
            if (!weapon) return;
           
            var damage = item.GetItemAttribute(vItemAttributes.Damage);
            var staminaCost = item.GetItemAttribute(vItemAttributes.StaminaCost);
            var defenseRate = item.GetItemAttribute(vItemAttributes.DefenseRate);
            var defenseRange = item.GetItemAttribute(vItemAttributes.DefenseRange);
            if (damage != null) this.weapon.damage.damageValue = damage.value;
            if (staminaCost != null) this.weapon.staminaCost = staminaCost.value;
            if (defenseRate != null) this.weapon.defenseRate = defenseRate.value;
            if (defenseRange != null) this.weapon.defenseRange = defenseRate.value;
        }

        public void OnUnequip(vItem item)
        {
            isEquiped = false;
        }
    }

}
