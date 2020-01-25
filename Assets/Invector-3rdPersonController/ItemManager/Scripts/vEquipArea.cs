using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;

namespace Invector.vItemManager
{
    public class vEquipArea : MonoBehaviour
    {
        public delegate void OnPickUpItem(vEquipArea area, vItemSlot slot);
        public OnPickUpItem onPickUpItemCallBack;

        public vInventory inventory;
        public vInventoryWindow rootWindow;
        public vItemWindow itemPicker;
        public Text itemtext;
        public List<vEquipSlot> equipSlots;
        public string equipPointName;
        public OnChangeEquipmentEvent onEquipItem;
        public OnChangeEquipmentEvent onUnequipItem;
        public OnSelectEquipArea onSelectEquipArea;
        [HideInInspector]
        public vEquipSlot currentSelectedSlot;
        [HideInInspector]
        public int indexOfEquipedItem;
        public vItem lastEquipedItem;
        private StringBuilder text;

        public void Init()
        {
            Start();
        }

        void Start()
        {
            inventory = GetComponentInParent<vInventory>();

            if (equipSlots.Count == 0)
            {
                var equipSlotsArray = GetComponentsInChildren<vEquipSlot>(true);
                equipSlots = equipSlotsArray.vToList();
            }
            rootWindow = GetComponentInParent<vInventoryWindow>();
            foreach (vEquipSlot slot in equipSlots)
            {
                slot.onSubmitSlotCallBack = OnSubmitSlot;
                slot.onSelectSlotCallBack = OnSelectSlot;
                slot.onDeselectSlotCallBack = OnDeselect;
                slot.amountText.text = "";
            }
        }

        public vItem currentEquipedItem
        {
            get
            {
                var validEquipSlots = ValidSlots;
                if (validEquipSlots.Count > 0) return validEquipSlots[indexOfEquipedItem].item;

                return null;
            }
        }

        public List<vEquipSlot> ValidSlots
        {
            get { return equipSlots.FindAll(slot => slot.isValid); }
        }

        public bool ContainsItem(vItem item)
        {
            return ValidSlots.Find(slot => slot.item == item) != null;
        }

        public void OnSubmitSlot(vItemSlot slot)
        {
            if (itemPicker != null)
            {
                currentSelectedSlot = slot as vEquipSlot;
                itemPicker.gameObject.SetActive(true);
                itemPicker.CreateEquipmentWindow(inventory.items, currentSelectedSlot.itemType, slot.item, OnPickItem);
            }
        }

        public void UnequipItem(vEquipSlot slot)
        {
            if (slot)
            {
                vItem item = slot.item;
                if (ValidSlots[indexOfEquipedItem].item == item)
                    lastEquipedItem = item;
                slot.RemoveItem();
                onUnequipItem.Invoke(this, item);
            }
        }

        public void UnequipItem(vItem item)
        {
            var slot = ValidSlots.Find(_slot => _slot.item == item);
            if (slot)
            {
                if (ValidSlots[indexOfEquipedItem].item == item) lastEquipedItem = item;
                slot.RemoveItem();
                onUnequipItem.Invoke(this, item);
            }
        }

        public void UnequipCurrentItem()
        {
            if (currentSelectedSlot)
            {
                var _item = currentSelectedSlot.item;
                if (ValidSlots[indexOfEquipedItem].item == _item) lastEquipedItem = _item;
                currentSelectedSlot.RemoveItem();
                onUnequipItem.Invoke(this, _item);
            }
        }

        public void OnSelectSlot(vItemSlot slot)
        {
            if (equipSlots.Contains(slot as vEquipSlot))
                currentSelectedSlot = slot as vEquipSlot;
            else currentSelectedSlot = null;

            onSelectEquipArea.Invoke(this);

            if (itemtext != null)
            {
                if (slot.item == null)
                {
                    itemtext.text = "";
                }
                else
                {
                    text = new StringBuilder();
                    text.Append(slot.item.name + "\n");
                    text.AppendLine(slot.item.description);
                    if (slot.item.attributes != null)
                        for (int i = 0; i < slot.item.attributes.Count; i++)
                        {
                            var _text = InsertSpaceBeforeUpperCase(slot.item.attributes[i].name.ToString());
                            text.AppendLine(_text + " : " + slot.item.attributes[i].value.ToString());
                        }

                    itemtext.text = text.ToString();
                }
            }
        }

        public string InsertSpaceBeforeUpperCase(string input)
        {
            var result = "";

            foreach (char c in input)
            {
                if (char.IsUpper(c))
                {
                    // if not the first letter, insert space before uppercase
                    if (!string.IsNullOrEmpty(result))
                    {
                        result += " ";
                    }
                }
                // start new word
                result += c;
            }

            return result;
        }

        public void OnDeselect(vItemSlot slot)
        {
            if (equipSlots.Contains(slot as vEquipSlot))
                currentSelectedSlot = null;
        }

        public void OnPickItem(vItemSlot slot)
        {
            if (onPickUpItemCallBack != null)
                onPickUpItemCallBack(this, slot);

            if (currentSelectedSlot.item != null && slot.item != currentSelectedSlot.item)
            {
                currentSelectedSlot.item.isInEquipArea = false;
                var item = currentSelectedSlot.item;
                if (item == slot.item) lastEquipedItem = item;
                currentSelectedSlot.RemoveItem();
                onUnequipItem.Invoke(this, item);
            }

            if (slot.item != currentSelectedSlot.item)
            {
                currentSelectedSlot.AddItem(slot.item);
                onEquipItem.Invoke(this, currentSelectedSlot.item);
            }
            itemPicker.gameObject.SetActive(false);
        }

        public void NextEquipSlot()
        {
            if (equipSlots == null || equipSlots.Count == 0) return;

            lastEquipedItem = currentEquipedItem;
            var validEquipSlots = ValidSlots;
            if (indexOfEquipedItem + 1 < validEquipSlots.Count)
                indexOfEquipedItem++;
            else
                indexOfEquipedItem = 0;

            if (currentEquipedItem != null)
                onEquipItem.Invoke(this, currentEquipedItem);
            onUnequipItem.Invoke(this, lastEquipedItem);
        }

        public void PreviousEquipSlot()
        {
            if (equipSlots == null || equipSlots.Count == 0) return;
            lastEquipedItem = currentEquipedItem;
            var validEquipSlots = ValidSlots;

            if (indexOfEquipedItem - 1 >= 0)
                indexOfEquipedItem--;
            else
                indexOfEquipedItem = validEquipSlots.Count - 1;

            if (currentEquipedItem != null)
                onEquipItem.Invoke(this, currentEquipedItem);

            onUnequipItem.Invoke(this, lastEquipedItem);
        }

        public void SetEquipSlot(int indexOfSlot)
        {
            if (equipSlots == null || equipSlots.Count == 0) return;


            if (indexOfSlot < equipSlots.Count /*&& equipSlots[index].isValid*/ && equipSlots[indexOfSlot].item != currentEquipedItem)
            {
                lastEquipedItem = currentEquipedItem;
                indexOfEquipedItem = indexOfSlot;
                if (currentEquipedItem != null)
                    onEquipItem.Invoke(this, currentEquipedItem);

                onUnequipItem.Invoke(this, lastEquipedItem);
            }
        }

        public void AddItemToEquipSlot(int indexOfSlot, vItem item)
        {
            if (indexOfSlot < equipSlots.Count && item != null)
            {

                var slot = equipSlots[indexOfSlot];

                if (slot != null && slot.isValid && slot.itemType.Contains(item.type))
                {
                    if (slot.item != null && slot.item != item)
                    {
                        if (currentEquipedItem == slot.item) lastEquipedItem = slot.item;
                        slot.item.isInEquipArea = false;
                        onUnequipItem.Invoke(this, item);
                    }
                    slot.AddItem(item);
                    onEquipItem.Invoke(this, item);
                }
            }
        }

        public void RemoveItemOfEquipSlot(int indexOfSlot)
        {
            if (indexOfSlot < equipSlots.Count)
            {
                var slot = equipSlots[indexOfSlot];
                if (slot != null && slot.item != null)
                {
                    var item = slot.item;
                    item.isInEquipArea = false;
                    if (currentEquipedItem == item) lastEquipedItem = currentEquipedItem;
                    slot.RemoveItem();
                    onUnequipItem.Invoke(this, item);
                }
            }
        }

        public void AddCurrentItem(vItem item)
        {
            if (indexOfEquipedItem < equipSlots.Count)
            {
                var slot = equipSlots[indexOfEquipedItem];
                if (slot.item != null && item != slot.item)
                {
                    if (currentEquipedItem == slot.item) lastEquipedItem = slot.item;
                    slot.item.isInEquipArea = false;
                    onUnequipItem.Invoke(this, currentSelectedSlot.item);
                }
                slot.AddItem(item);
                onEquipItem.Invoke(this, item);
            }
        }

        public void RemoveCurrentItem()
        {
            if (!currentEquipedItem) return;
            lastEquipedItem = currentEquipedItem;
            ValidSlots[indexOfEquipedItem].RemoveItem();
            onUnequipItem.Invoke(this, lastEquipedItem);
        }
    }
}
