using UnityEngine;

namespace Invector.vItemManager
{
    [vClassHeader("Remove Item", openClose = false)]
    public class vRemoveItem : vMonoBehaviour
    {
        public vRemoveCurrentItem.Type type = vRemoveCurrentItem.Type.DestroyItem;

        public bool getItemByName;
        [vHideInInspector("getItemByName")]
        public string itemName;
        [vHideInInspector("getItemByName", true)]
        public int itemID;

        public void RemoveItem(Collider target)
        {
            var itemManager = target.GetComponent<vItemManager>();
            RemoveItem(itemManager);
        }

        public void RemoveItem(GameObject target)
        {
            var itemManager = target.GetComponent<vItemManager>();
            RemoveItem(itemManager);
        }

        public void RemoveItem(vItemManager itemManager)
        {            
            if (itemManager)
            {
                var item = GetItem(itemManager);

                if(item != null)
                {
                    if (type == vRemoveCurrentItem.Type.UnequipItem)
                    {
                        itemManager.UnequipItem(item);
                    }
                    else if (type == vRemoveCurrentItem.Type.DestroyItem)
                    {
                        itemManager.DestroyItem(item, 1);
                    }
                    else
                    {
                        itemManager.DropItem(item, 1);
                    }
                }                
            }            
        }

        vItem GetItem(vItemManager itemManager)
        {
            if (getItemByName)
            {
                // VERIFY IF YOU HAVE A SPECIFIC ITEM IN YOUR INVENTORY
                if (itemManager.ContainItem(itemName))
                {
                    return itemManager.GetItem(itemName);
                }                
            }
            else
            {
                // VERIFY IF YOU HAVE A SPECIFIC ITEM IN YOUR INVENTORY
                if (itemManager.ContainItem(itemID))
                {
                    return itemManager.GetItem(itemID);
                }                
            }

            return null;
        }
    }
}