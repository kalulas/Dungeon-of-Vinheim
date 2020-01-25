using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Invector.vItemManager
{
    [vClassHeader("Use Item Event Trigger", useHelpBox = true, helpBoxText = "This script enable ItemUsage when TriggerEnter and disable onTriggerExit", openClose = false)]
    public class UseItemEventTrigger : vMonoBehaviour
    {
        public OnUseItemEvent itemEvent;

        protected vItemManager itemManager;
        [System.Serializable]
        public class OnUseItemEvent
        {
            internal vItem targetItem;
            public int id;
            [vHelpBox("Check this to enable the menu UI Button 'Use' on the Inventory Window")]
            public bool canUseWithOpenInventory;
            [vHelpBox("Override the Delay to use this Item")]
            public bool overrideItemUsageDelay;
            [vHideInInspector("overrideItemUsageDelay")]
            public float newDeleyTime;
            internal float defaultDelay;
            public UnityEngine.Events.UnityEvent onUse;
            public void OnOpenInventory(bool value)
            {
                if (canUseWithOpenInventory || !targetItem) return;

                targetItem.canBeUsed = !value;
            }
            public void ChangeItemUsageDelay()
            {
                if (!overrideItemUsageDelay || targetItem == null) return;
                defaultDelay = targetItem.enableDelayTime;
                targetItem.enableDelayTime = newDeleyTime;
            }

            public void ResetItemUsageDelay()
            {
                if (!overrideItemUsageDelay || targetItem == null) return;

                targetItem.enableDelayTime = defaultDelay;
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                itemManager = other.GetComponent<vItemManager>();
                if (itemManager)
                {
                    itemEvent.targetItem = itemManager.GetItem(itemEvent.id);
                    if (itemEvent.targetItem)
                    {
                        itemEvent.ChangeItemUsageDelay();
                        itemManager.onUseItem.AddListener(OnUseItem);
                        itemManager.onOpenCloseInventory.AddListener(itemEvent.OnOpenInventory);
                        itemEvent.targetItem.canBeUsed = true;
                    }
                }
            }
        }

        private void OnUseItem(vItem item)
        {
            if (itemManager && itemEvent.id == item.id)
            {
                itemManager.inventory.CloseInventory();
                itemManager.onUseItem.RemoveListener(OnUseItem);
                itemManager.onOpenCloseInventory.RemoveListener(itemEvent.OnOpenInventory);
                itemEvent.onUse.Invoke();
                itemEvent.ResetItemUsageDelay();
                itemEvent.targetItem = null;
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                if (itemManager)
                {
                    itemManager.onUseItem.RemoveListener(OnUseItem);
                    itemManager.onOpenCloseInventory.RemoveListener(itemEvent.OnOpenInventory);
                    if (itemEvent.targetItem)
                    {
                        itemEvent.targetItem.canBeUsed = false;
                        itemEvent.ResetItemUsageDelay();
                        itemEvent.targetItem = null;
                    }
                }
            }
        }
    }
}