using Invector.vCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Invector.vItemManager
{
    [vClassHeader("Weapon Holder Manager", "Create a new empty object inside a bone and add the vWeaponHolder script")]
    public class vWeaponHolderManager : vMonoBehaviour
    {
        public vWeaponHolder[] holders = new vWeaponHolder[0];
        public bool debugMode;
        internal bool inEquip;
        internal bool inUnequip;
        internal vItemManager itemManager;
        internal vThirdPersonController cc;
        public Dictionary<string, List<vWeaponHolder>> holderAreas = new Dictionary<string, List<vWeaponHolder>>();
        protected float equipTime;
        private float currentUnsheatheTimer;
        float timeOut;

        void OnDrawGizmosSelected()
        {
            holders = GetComponentsInChildren<vWeaponHolder>(true);
        }

        protected virtual bool IsEquipping
        {
            get
            {
                if (cc) return cc.IsAnimatorTag("IsEquipping");
                return false;
            }
        }

        void Start()
        {
            itemManager = GetComponent<vItemManager>();
            cc = GetComponent<vThirdPersonController>();
            if (itemManager)
            {
                itemManager.onEquipItem.AddListener(EquipWeapon);
                itemManager.onUnequipItem.AddListener(UnequipWeapon);

                holders = GetComponentsInChildren<vWeaponHolder>(true);
                if (holders != null)
                {
                    foreach (vWeaponHolder holder in holders)
                    {
                        if (!holderAreas.ContainsKey(holder.equipPointName))
                        {
                            holderAreas.Add(holder.equipPointName, new List<vWeaponHolder>());
                            holderAreas[holder.equipPointName].Add(holder);
                        }
                        else
                        {
                            holderAreas[holder.equipPointName].Add(holder);
                        }

                        holder.SetActiveHolder(false);
                        holder.SetActiveWeapon(false);
                    }
                }
            }
        }

        public void EquipWeapon(vEquipArea equipArea, vItem item)
        {
            if (item == null) return;
            var slotsInArea = equipArea.ValidSlots;

            if (slotsInArea != null && slotsInArea.Count > 0 && holderAreas.ContainsKey(equipArea.equipPointName))
            {
                // Check All Holders to Display
                for (int i = 0; i < slotsInArea.Count; i++)
                {
                    if (slotsInArea[i].item != null)
                    {
                        var holder = holderAreas[equipArea.equipPointName].Find(h => slotsInArea[i].item && slotsInArea[i].item.id == h.itemID
                        && ((equipArea.currentEquipedItem != null
                        && equipArea.currentEquipedItem != item
                        && equipArea.currentEquipedItem != slotsInArea[i].item
                        && equipArea.currentEquipedItem.id != item.id) || equipArea.currentEquipedItem == null));

                        if (holder)
                        {
                            holder.SetActiveHolder(true);
                            holder.SetActiveWeapon(true);
                            if (debugMode) Debug.Log("Hold: " + slotsInArea[i].item);
                        }
                    }
                }
                // Check Current Item to Equip with time
                if (equipArea.currentEquipedItem != null && equipArea.currentEquipedItem == item)
                {

                    var holder = holderAreas[equipArea.equipPointName].Find(h => h.itemID == equipArea.currentEquipedItem.id);
                    if (holder)
                    {
                        // Unhide Holder and hide Equiped weapon
                        var immediate = (equipArea.currentEquipedItem != item || itemManager.inventory != null && itemManager.inventory.isOpen) || string.IsNullOrEmpty(equipArea.currentEquipedItem.EnableAnim);
                        if (debugMode) Debug.Log("UnHold: " + item.name);
                        StartCoroutine(EquipRoutine(equipArea.currentEquipedItem.enableDelayTime, immediate,
                           () => { holder.SetActiveHolder(true); }, () => { holder.SetActiveWeapon(false); }, item.name));
                    }
                }
            }
        }

        public void UnequipWeapon(vEquipArea equipArea, vItem item)
        {
            if (holders.Length == 0 || item == null) return;
            if ((itemManager.inventory != null) && holderAreas.ContainsKey(equipArea.equipPointName))
            {
                var holder = holderAreas[equipArea.equipPointName].Find(h => item.id == h.itemID);
                if (holder)
                {
                    // Check if EquipArea contains unequipped item
                    var containsItem = equipArea.ValidSlots.Find(slot => slot.item == item) != null;
                    if (debugMode) Debug.Log(containsItem ? "Hold: " + item.name : "Hide :" + item.name + " Holder");

                    // Hide or unhide holder and weapon if contains item
                    if (containsItem)
                    {
                        var immediate = (itemManager.inventory != null && itemManager.inventory.isOpen) || string.IsNullOrEmpty(item.DisableAnim);

                        StartCoroutine(UnequipRoutine(item.disableDelayTime, immediate,
                               () => { holder.SetActiveHolder(containsItem); }, () => { holder.SetActiveWeapon(containsItem); }, item.name));
                    }
                    else
                    {
                        holder.SetActiveHolder(false);
                        holder.SetActiveWeapon(false);
                    }
                }
            }
        }

        internal vWeaponHolder GetHolder(GameObject equipment, int id)
        {
            var equipPoint = itemManager.equipPoints.Find(e => e.equipmentReference != null
                                                          && e.equipmentReference.item && e.equipmentReference.item.id == id
                                                          && e.equipmentReference.equipedObject == equipment);
            if (holderAreas.ContainsKey(equipPoint.equipPointName))
            {
                var holder = holderAreas[equipPoint.equipPointName].Find(h => id == h.itemID);
                return holder;
            }
            else
            {
                if (debugMode) Debug.LogWarning(this.ToString() + " fail to find a holder with equipPointName " + equipPoint.equipPointName);
                return null;
            }
        }

        internal IEnumerator UnequipRoutine(float equipDelay, bool immediate = false, UnityEngine.Events.UnityAction onStart = null, UnityEngine.Events.UnityAction onFinish = null, string itemName = "")
        {
            if (debugMode) Debug.Log("Start Unequip: " + itemName);
            if (!immediate && !inEquip) inUnequip = true;

            while (!IsEquipping && !immediate)
            {
                yield return null;
            }
            if (onStart != null)
                onStart.Invoke();

            if (!inEquip && !immediate) // ignore time if inEquip or immediate unequip
            {
                var equipTime = equipDelay;
                while (!immediate && !inEquip && equipTime > 0f)
                {
                    equipTime -= Time.deltaTime;
                    yield return null;
                }
            }
            inUnequip = false;
            if (onFinish != null)
                onFinish.Invoke();
            if (debugMode) Debug.Log("Finish Unequip: " + itemName);
        }

        internal IEnumerator EquipRoutine(float equipDelay, bool immediate = false, UnityEngine.Events.UnityAction onStart = null, UnityEngine.Events.UnityAction onFinish = null, string itemName = "")
        {
            if (debugMode) Debug.Log("Start Equip: " + itemName);
            if (!immediate)
                inEquip = true;
            while (!IsEquipping && !immediate)
            {
                yield return null;
            }
            if (onStart != null) onStart.Invoke();
            if (!inUnequip && !immediate) // ignore time if inEquip or immediate unequip
            {
                var equipTime = equipDelay;
                while (!immediate && !inUnequip && equipTime > 0f)
                {
                    equipTime -= Time.deltaTime;
                    yield return null;
                }
            }
            inEquip = false;
            if (onFinish != null) onFinish.Invoke();
            if (debugMode) Debug.Log("Finish Equip: " + itemName);
        }
    }
}
