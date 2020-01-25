using System.Collections.Generic;

namespace Invector.vItemManager
{
    [vClassHeader("Check If Item Is Equipped")]
    public class vCheckItemIsEquipped : vMonoBehaviour
    {
        public vItemManager itemManager;
        public bool getInParent = true;
        public List<CheckEvent> itemChecks;

        void Awake()
        {
            if (!itemManager)
            {
                if(getInParent)
                    itemManager = GetComponentInParent<vItemManager>();
                else
                    itemManager = GetComponent<vItemManager>();
                itemManager.onEquipItem.AddListener(CheckIsEquipped);
                itemManager.onUnequipItem.AddListener(CheckIsEquipped);
            }
        }

        private void CheckIsEquipped(vEquipArea arg0, vItem arg1)
        {
            for (int i = 0; i < itemChecks.Count; i++)
            {
                CheckEvent check = itemChecks[i];
                CheckItem(check);
            }
        }

        private void CheckItem(CheckEvent check)
        {
            bool _isEquipped = itemManager.ItemIsEquiped(check._itemID);
            if (_isEquipped != check.isEquipped)
            {
                check.isEquipped = _isEquipped;
                if (check.isEquipped)
                    check.onIsItemEquipped.Invoke();
                else
                    check.onIsItemUnequipped.Invoke();
            }
        }

        [System.Serializable]
        public class CheckEvent
        {
            public int _itemID;
            public UnityEngine.Events.UnityEvent onIsItemEquipped, onIsItemUnequipped;
            internal bool isEquipped;
        }
    }
}