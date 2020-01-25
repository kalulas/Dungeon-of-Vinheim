using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Invector.vItemManager
{
    [vClassHeader("Item Options Window")]
    public class vItemOptionWindow : vMonoBehaviour
    {
        public Button useItemButton;
        public Button dropItemButton;
        public Button destroyItemButton;      
    
        public virtual void EnableOptions(vItemSlot slot)
        {
            //if (slot ==null || slot.item==null) return;
            //useItemButton.interactable = itemsCanBeUsed.Contains(slot.item.type);
        }

        protected virtual void ValidateButtons(vItem item, out bool result)
        {
            useItemButton.interactable = item.canBeUsed;
            dropItemButton.interactable = item.canBeDroped;
            destroyItemButton.interactable =  item.canBeDestroyed;
            result = useItemButton.interactable || useItemButton.interactable || destroyItemButton.interactable;
        }

        public virtual bool CanOpenOptions(vItem item)
        {
            if (item == null) return false;
            var canOpen = false;
            ValidateButtons(item, out canOpen);
            return canOpen;
        }      
    }
}

