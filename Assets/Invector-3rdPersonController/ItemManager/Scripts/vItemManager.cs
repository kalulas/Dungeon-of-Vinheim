using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Invector.vItemManager
{
    using Invector.vCharacterController;
    using vCharacterController.vActions;

    [vClassHeader("ItemManager")]
    public partial class vItemManager : vMonoBehaviour, IActionReceiver
    {
        public delegate void CanUseItemDelegate(vItem item, ref List<bool> validationList);
        //  [vEditorToolbar("Settings")]
        public bool dropItemsWhenDead;

        public vInventory inventoryPrefab;
        [HideInInspector]
        public vInventory inventory;
        public vItemListData itemListData;
        [Header("---Items Filter---")]
        public List<vItemType> itemsFilter = new List<vItemType>() { 0 };

        #region SerializedProperties in Custom Editor
        [SerializeField]
        public List<ItemReference> startItems = new List<ItemReference>();

        public List<vItem> items;

        //   [vEditorToolbar("Events")]
        public OnHandleItemEvent onStartItemUsage, onUseItem, onUseItemFail, onAddItem, onChangeItemAmount;

        public OnChangeItemAmount onLeaveItem, onDropItem;
        public OnOpenCloseInventory onOpenCloseInventory;
        public OnChangeEquipmentEvent onEquipItem, onUnequipItem;
        [SerializeField]
        //  [vEditorToolbar("Equip Points")]
        public List<EquipPoint> equipPoints;
        [SerializeField]
        //  [vEditorToolbar("Attribute Events")]
        public List<ApplyAttributeEvent> applyAttributeEvents;
        #endregion

        internal bool inEquip;
        internal bool usingItem;
        private float equipTimer;
        private Animator animator;
        private static vItemManager instance;

        public event CanUseItemDelegate canUseItemDelegate;

        public class ItemClass
        {
            public string name;
            public List<vItem> items;
            public List<vItemType> suportType;
            public bool limited;
            public int maxSlots;
        }        

        IEnumerator Start()
        {
            if (instance == null)
            {
                inventory = FindObjectOfType<vInventory>();
                instance = this;

                var melee = GetComponent<vMeleeCombatInput>();

                if (!inventory && inventoryPrefab)
                    inventory = Instantiate(inventoryPrefab);

                if (!inventory) Debug.LogError("No vInventory assigned!");

                if (inventory)
                {
                    inventory.GetItemsHandler = GetItems;
                    inventory.GetItemsAllHandler = GetAllItems;
                    if (melee)
                        inventory.IsLockedEvent = () => { return melee.lockInventory; }; //Locking Inventory Input if MeleeInput is lockInventory
                    inventory.onEquipItem.AddListener(EquipItem);
                    inventory.onUnequipItem.AddListener(UnequipItem);
                    inventory.onDropItem.AddListener(DropItem);
                    inventory.onLeaveItem.AddListener(DestroyItem);
                    inventory.onUseItem.AddListener(UseItem);
                    inventory.onOpenCloseInventory.AddListener(OnOpenCloseInventory);
                }
                animator = GetComponent<Animator>();

                if (dropItemsWhenDead)
                {
                    var character = GetComponent<vCharacterController.vICharacter>();
                    if (character != null)
                        character.onDead.AddListener(DropAllItens);
                }
                yield return new WaitForEndOfFrame();
                items = new List<vItem>();
                if (itemListData)
                {
                    for (int i = 0; i < startItems.Count; i++)
                    {
                        AddItem(startItems[i], true);
                    }
                }
            }
        }

        public virtual void LockInventoryInput(bool value)
        {
            if (inventory)
                inventory.lockInventoryInput = value;
        }

        public virtual List<vItem> GetItems()
        {
            return items;
        }

        public virtual List<vItem> GetAllItems()
        {
            return itemListData ? itemListData.items : null;
        }

        /// <summary>
        /// Add new Instance of Item to itemList
        /// </summary>
        /// <param name="item"></param>
        public virtual void AddItem(ItemReference itemReference, bool immediate = false)
        {
            if (itemReference != null && itemListData != null && itemListData.items.Count > 0)
            {
                var item = itemListData.items.Find(t => t.id.Equals(itemReference.id));
                if (item)
                {
                    var sameItems = items.FindAll(i => i.stackable && i.id == item.id && i.amount < i.maxStack);
                    if (sameItems.Count == 0)
                    {
                        var _item = Instantiate(item);
                        _item.name = _item.name.Replace("(Clone)", string.Empty);

                        if (itemReference.attributes != null && _item.attributes != null && item.attributes.Count == itemReference.attributes.Count)
                            _item.attributes = new List<vItemAttribute>(itemReference.attributes);

                        _item.amount = 0;

                        for (int i = 0; i < item.maxStack && _item.amount < _item.maxStack && itemReference.amount > 0; i++)
                        {
                            _item.amount++;
                            itemReference.amount--;
                        }
                        items.Add(_item);
                        onAddItem.Invoke(_item);

                        if (itemReference.autoEquip)
                        {
                            itemReference.autoEquip = false;
                            AutoEquipItem(_item, itemReference.indexArea, immediate);
                        }

                        if (itemReference.amount > 0) AddItem(itemReference);
                    }
                    else
                    {
                        var indexOffItem = items.IndexOf(sameItems[0]);

                        for (int i = 0; i < items[indexOffItem].maxStack && items[indexOffItem].amount < items[indexOffItem].maxStack && itemReference.amount > 0; i++)
                        {
                            items[indexOffItem].amount++;
                            itemReference.amount--;
                            onChangeItemAmount.Invoke(items[indexOffItem]);
                        }

                        if (itemReference.amount > 0)
                            AddItem(itemReference);
                    }
                }
            }
        }

        public bool CanUseItem(vItem item)
        {
            if (canUseItemDelegate != null)
            {
                List<bool> canUse = new List<bool>();
                canUseItemDelegate.Invoke(item, ref canUse);
                return !canUse.Contains(false);
            }
            return item.canBeUsed;
        }

        public virtual void UseItem(vItem item)
        {
            if (item)
            {
                if (CanUseItem(item))
                {
                    StartCoroutine(UseItemRoutine(item));
                }
                else onUseItemFail.Invoke(item);
            }
        }
        public IEnumerator UseItemRoutine(vItem item)
        {
            usingItem = true;
            LockInventoryInput(true);
            onStartItemUsage.Invoke(item);
            var canUse = CanUseItem(item);
            yield return null;
            if (canUse)
            {
                var time = item.enableDelayTime;

                if (!inventory.isOpen)
                {
                    animator.SetBool("FlipAnimation", false);
                    animator.CrossFade(item.EnableAnim, 0.25f);

                    while (usingItem && time > 0 && canUse)
                    {
                        canUse = CanUseItem(item);
                        time -= Time.deltaTime;
                        yield return null;
                    }
                }
                if (usingItem && canUse)
                {
                    if (item.destroyAfterUse) item.amount--;
                    onUseItem.Invoke(item);
                    if (item.attributes != null && item.attributes.Count > 0 && applyAttributeEvents.Count > 0)
                    {
                        foreach (ApplyAttributeEvent attributeEvent in applyAttributeEvents)
                        {
                            var attributes = item.attributes.FindAll(a => a.name.Equals(attributeEvent.attribute));
                            foreach (vItemAttribute attribute in attributes)
                                attributeEvent.onApplyAttribute.Invoke(attribute.value);
                        }
                    }
                    if (item.destroyAfterUse && item.amount <= 0 && items.Contains(item))
                    {
                        items.Remove(item);
                        Destroy(item);
                    }
                    usingItem = false;
                    inventory.CheckEquipmentChanges();
                }
                else onUseItemFail.Invoke(item);
            }
            else onUseItemFail.Invoke(item);
            LockInventoryInput(false);
        }

        public virtual void DestroyItem(vItem item, int amount)
        {
            onLeaveItem.Invoke(item, amount);
            item.amount -= amount;

            if (item.amount <= 0 && items.Contains(item))
            {
                var equipArea = System.Array.Find(inventory.equipAreas, e => e.ValidSlots.Exists(s => s.item != null && s.item.id.Equals(item.id)));

                if (equipArea != null)
                {
                    equipArea.UnequipItem(item);
                }
                items.Remove(item);
                Destroy(item);
            }
        }

        public virtual void DropItem(vItem item, int amount)
        {
            item.amount -= amount;
            if (item.dropObject != null)
            {
                var dropObject = Instantiate(item.dropObject, transform.position, transform.rotation) as GameObject;
                vItemCollection collection = dropObject.GetComponent<vItemCollection>();
                if (collection != null)
                {
                    collection.items.Clear();
                    var itemReference = new ItemReference(item.id);
                    itemReference.amount = amount;
                    itemReference.attributes = new List<vItemAttribute>(item.attributes);
                    collection.items.Add(itemReference);
                }
            }
            onDropItem.Invoke(item, amount);
            if (item.amount <= 0 && items.Contains(item))
            {
                var equipArea = System.Array.Find(inventory.equipAreas, e => e.ValidSlots.Exists(s => s.item != null && s.item.id.Equals(item.id)));

                if (equipArea != null)
                {
                    equipArea.UnequipItem(item);
                }
                items.Remove(item);
                Destroy(item);
            }
        }

        public virtual void DropAllItens(GameObject target = null)
        {
            if (target != null && target != gameObject) return;
            List<ItemReference> itemReferences = new List<ItemReference>();
            for (int i = 0; i < items.Count; i++)
            {
                if (itemReferences.Find(_item => _item.id == items[i].id) == null)
                {
                    var sameItens = items.FindAll(_item => _item.id == items[i].id);
                    ItemReference itemReference = new ItemReference(items[i].id);
                    for (int a = 0; a < sameItens.Count; a++)
                    {
                        if (sameItens[a].type != vItemType.Consumable)
                        {
                            var equipPoint = equipPoints.Find(ep => ep.equipmentReference.item == sameItens[a]);
                            if (equipPoint != null && equipPoint.equipmentReference.equipedObject != null)
                                UnequipItem(equipPoint.area, equipPoint.equipmentReference.item);
                        }
                        else
                        {
                            var equipArea = System.Array.Find(inventory.equipAreas, e => e.ValidSlots.Exists(s => s.item != null && s.item.id.Equals(sameItens[a].id)));

                            if (equipArea != null)
                            {
                                equipArea.UnequipItem(sameItens[a]);
                            }
                        }

                        itemReference.amount += sameItens[a].amount;
                        Destroy(sameItens[a]);
                    }
                    itemReferences.Add(itemReference);
                    if (equipPoints != null)
                    {
                        var equipPoint = equipPoints.Find(e => e.equipmentReference != null && e.equipmentReference.item != null && e.equipmentReference.item.id == itemReference.id && e.equipmentReference.equipedObject != null);
                        if (equipPoint != null)
                        {
                            Destroy(equipPoint.equipmentReference.equipedObject);
                            equipPoint.equipmentReference = null;
                        }
                    }
                    if (items[i].dropObject)
                    {
                        var dropObject = Instantiate(items[i].dropObject, transform.position, transform.rotation) as GameObject;
                        vItemCollection collection = dropObject.GetComponent<vItemCollection>();
                        if (collection != null)
                        {
                            collection.items.Clear();
                            collection.items.Add(itemReference);
                        }
                    }
                }
            }
            items.Clear();
        }

        #region Check Items

        /// <summary>
        /// Check if Item List contains a  Item
        /// </summary>
        /// <param name="id">Item id</param>
        /// <returns></returns>
        public virtual bool ContainItem(int id)
        {
            return items.Exists(i => i.id == id);
        }

        /// <summary>
        /// Check if Item List contains a  Item
        /// </summary>
        /// <param name="id">Item name</param>
        /// <returns></returns>
        public virtual bool ContainItem(string itemName)
        {
            return items.Exists(i => i.name == itemName);
        }

        /// <summary>
        /// Check if the list contains a item with certain amount, or more
        /// </summary>
        /// <param name="id">Item id</param>
        /// <param name="amount">Item amount</param>
        /// <returns></returns>
        public virtual bool ContainItem(int id, int amount)
        {
            var item = items.Find(i => i.id == id && i.amount >= amount);
            return item != null;
        }

        /// <summary>
        /// Check if the list contains a item with certain amount, or more
        /// </summary>
        /// <param name="id">Item name</param>
        /// <param name="amount">Item amount</param>
        /// <returns></returns>
        public virtual bool ContainItem(string itemName, int amount)
        {
            var item = items.Find(i => i.name == itemName && i.amount >= amount);
            return item != null;
        }

        /// <summary>
        /// Check if the list contains a certain count of items, or more
        /// </summary>
        /// <param name="id">Item id</param>
        /// <param name="count">Item count</param>
        /// <returns></returns>
        public virtual bool ContainItems(int id, int count)
        {
            var _items = items.FindAll(i => i.id == id);
            return _items != null && _items.Count >= count;
        }

        /// <summary>
        /// Check if the list contains a certain count of items, or more
        /// </summary>
        /// <param name="id">Item name</param>
        /// <param name="count">Item count</param>
        /// <returns></returns>
        public virtual bool ContainItems(string itemName, int count)
        {
            var _items = items.FindAll(i => i.name == itemName);
            return _items != null && _items.Count >= count;
        }

        /// <summary>
        /// Check if EquipArea Contains some item
        /// <seealso cref="vInventory.equipAreas"/>
        /// </summary>
        /// <param name="indexOfArea">index of equip area</param>
        /// <returns></returns>
        public virtual bool EquipAreaHasSomeItem(int indexOfArea)
        {
            var equipArea = inventory.equipAreas[indexOfArea];
            return equipArea.equipSlots.Exists(slot => slot.item != null);
        }

        /// <summary>
        /// Check if item is in Some area
        /// <seealso cref="vInventory.equipAreas"/>
        /// </summary>
        /// <param name="id">Item id</param>
        /// <returns></returns>
        public virtual bool ItemIsInSomeEquipArea(int id)
        {
            if (!inventory || inventory.equipAreas.Length == 0) return false;

            for (int i = 0; i < inventory.equipAreas.Length; i++)
            {
                var equipArea = inventory.equipAreas[i];
                if (equipArea.equipSlots.Exists(slot => slot.item.id.Equals(id)))
                    return true;

            }
            return false;
        }

        /// <summary>
        /// Check if item is in Some area
        /// <seealso cref="vInventory.equipAreas"/>
        /// </summary>
        /// <param name="id">Item name</param>
        /// <returns></returns>
        public virtual bool ItemIsInSomeEquipArea(string itemName)
        {
            if (!inventory || inventory.equipAreas.Length == 0) return false;

            for (int i = 0; i < inventory.equipAreas.Length; i++)
            {
                var equipArea = inventory.equipAreas[i];
                if (equipArea.equipSlots.Exists(slot => slot.item.name.Equals(itemName)))
                    return true;

            }
            return false;
        }

        /// <summary>
        /// Check if item is in Specific area
        /// <seealso cref="vInventory.equipAreas"/>
        /// </summary>
        /// <param name="id">Item id</param>
        /// <param name="indexOfArea">index of equip area </param>
        ///
        /// <returns></returns>
        public virtual bool ItemIsInSpecificEquipArea(int id, int indexOfArea)
        {
            if (!inventory || inventory.equipAreas.Length == 0 || indexOfArea > inventory.equipAreas.Length - 1) return false;
            var equipArea = inventory.equipAreas[indexOfArea];
            if (equipArea.equipSlots.Exists(slot => slot.item.id.Equals(id)))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Check if item is in Specific area
        /// <seealso cref="vInventory.equipAreas"/>
        /// </summary>
        /// <param name="id">Item name</param>
        /// <param name="indexOfArea">index of equip area </param>
        ///
        /// <returns></returns>
        public virtual bool ItemIsInSpecificEquipArea(string itemName, int indexOfArea)
        {
            if (!inventory || inventory.equipAreas.Length == 0 || indexOfArea > inventory.equipAreas.Length - 1) return false;
            var equipArea = inventory.equipAreas[indexOfArea];
            if (equipArea.equipSlots.Exists(slot => slot.item.name.Equals(itemName)))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Check if EquipPoint Contains some item  
        /// <seealso cref="vItemManager.equipPoints"/>
        /// </summary>
        /// <param name="equipPointName">EquipPoint name</param>
        /// <returns></returns>
        public virtual bool EquipPointHasSomeItem(string equipPointName)
        {
            return equipPoints.Exists(ep => ep.equipPointName.Equals(equipPointName) && ep.equipmentReference != null && ep.equipmentReference.item != null);
        }

        /// <summary>
        /// Check if the item is equipped in some EquipPoint
        /// <seealso cref="vItemManager.equipPoints"/>
        /// </summary>
        /// <param name="id">Item id</param>
        /// <returns></returns>
        public virtual bool ItemIsInSomeEquipPont(int id)
        {
            return equipPoints.Exists(ep => ep.equipmentReference != null && ep.equipmentReference.item != null && ep.equipmentReference.item.id.Equals(id));
        }

        /// <summary>
        /// Check if the item is equipped in some EquipPoint
        /// <seealso cref="vItemManager.equipPoints"/>
        /// </summary>
        /// <param name="id">Item name</param>
        /// <returns></returns>
        public virtual bool ItemIsInSomeEquipPont(string itemName)
        {
            return equipPoints.Exists(ep => ep.equipmentReference != null && ep.equipmentReference.item != null && ep.equipmentReference.item.name.Equals(itemName));
        }

        /// <summary>
        /// Check if item is equipped in specific EquipPoint
        /// <seealso cref="vItemManager.equipPoints"/>
        /// </summary>       
        /// <param name="id">Item id</param>
        /// <param name="equipPointName">EquipPoint name</param>
        /// <returns></returns>
        public virtual bool ItemIsInSpecificEquipPoint(int id, string equipPointName)
        {
            return equipPoints.Exists(ep => ep.equipPointName.Equals(equipPointName) && ep.equipmentReference != null && ep.equipmentReference.item != null && ep.equipmentReference.item.id.Equals(id));
        }

        /// <summary>
        /// Check if item is equipped in specific EquipPoint
        /// <seealso cref="vItemManager.equipPoints"/>
        /// </summary>       
        /// <param name="id">Item name</param>
        /// <param name="equipPointName">EquipPoint name</param>
        /// <returns></returns>
        public virtual bool ItemIsInSpecificEquipPoint(string itemName, string equipPointName)
        {
            return equipPoints.Exists(ep => ep.equipPointName.Equals(equipPointName) && ep.equipmentReference != null && ep.equipmentReference.item != null && ep.equipmentReference.item.name.Equals(itemName));
        }

        #endregion

        #region Get Items 

        /// <summary>
        /// Get a single Item with same id
        /// <seealso cref="vItemManager.items"/>
        /// </summary>
        /// <param name="id">Item id</param>
        /// <returns></returns>
        public virtual vItem GetItem(int id)
        {
            return items.Find(i => i.id == id);
        }

        /// <summary>
        /// Get a single Item with same name
        /// <seealso cref="vItemManager.items"/>
        /// </summary>
        /// <param name="id">Item name</param>
        /// <returns></returns>
        public virtual vItem GetItem(string itemName)
        {
            return items.Find(i => i.name == itemName);
        }

        /// <summary>
        /// Get item of the equipPoint
        /// </summary>
        /// <param name="equipPointName">EquipPoint name</param>
        /// <returns></returns>
        public virtual vItem GetItemInEquipPoint(string equipPointName)
        {
            var equipPoint = equipPoints.Find(ep => ep.equipPointName.Equals(equipPointName));
            if (equipPoint != null && equipPoint.equipmentReference != null && equipPoint.equipmentReference.item) return equipPoint.equipmentReference.item;
            else return null;
        }

        /// <summary>
        /// Get All Items with same id
        /// <seealso cref="vItemManager.items"/>
        /// </summary>
        /// <param name="id">Item id</param>
        /// <returns></returns>
        public virtual List<vItem> GetItems(int id)
        {
            var _items = items.FindAll(i => i.id == id);
            return _items;
        }

        /// <summary>
        /// Get All Items with same name
        /// <seealso cref="vItemManager.items"/>
        /// </summary>
        /// <param name="id">Item id</param>
        /// <returns></returns>
        public virtual List<vItem> GetItems(string itemName)
        {
            var _items = items.FindAll(i => i.name == itemName);
            return _items;
        }

        /// <summary>
        /// Get All Items in EquiArea
        /// <seealso cref="vInventory.equipAreas"/>
        /// </summary>
        /// <param name="indexOfArea">index of equip area</param>
        /// <returns></returns>
        public virtual List<vItem> GetItemsInEquipArea(int indexOfArea)
        {
            var list = new List<vItem>();
            if (!inventory || inventory.equipAreas.Length == 0 || indexOfArea > inventory.equipAreas.Length - 1) return list;
            var equipArea = inventory.equipAreas[indexOfArea];
            var validSlot = equipArea.ValidSlots;
            for (int i = 0; i < validSlot.Count; i++)
            {
                if (validSlot[i].item != null) list.Add(validSlot[i].item);
            }
            return list;
        }

        /// <summary>
        /// Get All Item in all EquipAreas
        /// <seealso cref="vInventory.equipAreas"/>
        /// </summary>
        /// <returns></returns>
        public virtual List<vItem> GetAllItemInAllEquipAreas()
        {
            var list = new List<vItem>();
            if (!inventory || inventory.equipAreas.Length == 0) return list;
            for (int i = 0; i < inventory.equipAreas.Length; i++)
            {
                var equipArea = inventory.equipAreas[i];
                var validSlot = equipArea.ValidSlots;
                for (int a = 0; a < validSlot.Count; a++)
                {
                    if (validSlot[a].item != null) list.Add(validSlot[a].item);
                }
            }
            return list;
        }

        #endregion

        public virtual void EquipItem(vEquipArea equipArea, vItem item)
        {
            onEquipItem.Invoke(equipArea, item);
            if (item != equipArea.currentEquipedItem) return;

            var equipPoint = equipPoints.Find(ep => ep.equipPointName == equipArea.equipPointName);
            if (equipPoint != null && item != null && equipPoint.equipmentReference.item != item)
            {
                equipTimer = item.enableDelayTime;

                var type = item.type;
                if (type != vItemType.Consumable)
                {
                    if (!inventory.isOpen)
                    {
                        animator.SetBool("FlipEquip", equipArea.equipPointName.Contains("Left"));
                        animator.CrossFade(item.EnableAnim, 0.25f);
                    }
                    equipPoint.area = equipArea;
                    StartCoroutine(EquipItemRoutine(equipPoint, item));
                }
            }
        }

        public virtual void UnequipItem(vEquipArea equipArea, vItem item)
        {
            onUnequipItem.Invoke(equipArea, item);

            //if (item != equipArea.lastEquipedItem) return;
            var equipPoint = equipPoints.Find(ep => ep.equipPointName == equipArea.equipPointName && ep.equipmentReference.item != null && ep.equipmentReference.item == item);
            if (equipPoint != null && item != null)
            {
                equipTimer = item.disableDelayTime;
                var type = item.type;
                if (type != vItemType.Consumable)
                {
                    if (!inventory.isOpen && !inEquip && equipPoint.equipmentReference.equipedObject.activeInHierarchy)
                    {
                        animator.SetBool("FlipEquip", equipArea.equipPointName.Contains("Left"));
                        animator.CrossFade(item.DisableAnim, 0.25f);
                    }
                    StartCoroutine(UnequipItemRoutine(equipPoint, item));
                }
            }
        }

        public virtual void UnequipItem(vItem item)
        {
            var equipArea = System.Array.Find(inventory.equipAreas, e => e.ValidSlots.Exists(s => s.item != null && s.item.id.Equals(item.id)));
            if (equipArea != null)
            {
                onUnequipItem.Invoke(equipArea, item);

                //if (item != equipArea.lastEquipedItem) return;
                var equipPoint = equipPoints.Find(ep => ep.equipPointName == equipArea.equipPointName && ep.equipmentReference.item != null && ep.equipmentReference.item == item);
                if (equipPoint != null && item != null)
                {
                    equipTimer = item.enableDelayTime;
                    var type = item.type;
                    if (type != vItemType.Consumable)
                    {
                        if (!inventory.isOpen && !inEquip)
                        {
                            animator.SetBool("FlipAnimation", equipArea.equipPointName.Contains("Left"));
                            animator.CrossFade(item.DisableAnim, 0.25f);
                        }
                        StartCoroutine(UnequipItemRoutine(equipPoint, item));
                    }
                }
            }
        }

        IEnumerator EquipItemRoutine(EquipPoint equipPoint, vItem item)
        {
            LockInventoryInput(true);
            if (!inEquip)
            {
                inventory.canEquip = false;
                inEquip = true;

                if (equipPoint != null)
                {
                    if (item.originalObject)
                    {
                        if (equipPoint.equipmentReference != null && equipPoint.equipmentReference.equipedObject != null)
                        {
                            var _equipmentsA = equipPoint.equipmentReference.equipedObject.GetComponents<vIEquipment>();
                            for (int i = 0; i < _equipmentsA.Length; i++)
                            {
                                if (_equipmentsA[i] != null)
                                {
                                    _equipmentsA[i].OnUnequip(equipPoint.equipmentReference.item);
                                    _equipmentsA[i].equipPoint = null;
                                }
                            }
                            Destroy(equipPoint.equipmentReference.equipedObject);
                        }
                        if (!inventory.isOpen && !string.IsNullOrEmpty(item.EnableAnim))
                        {
                            while (equipTimer > 0)
                            {
                                if (item == null) break;
                                yield return null;
                                equipTimer -= Time.deltaTime;
                            }
                        }
                        var point = equipPoint.handler.customHandlers.Find(p => p.name == item.customHandler);
                        var equipTransform = point != null ? point : equipPoint.handler.defaultHandler;
                        var equipedObject = Instantiate(item.originalObject, equipTransform.position, equipTransform.rotation) as GameObject;

                        equipedObject.transform.parent = equipTransform;

                        if (equipPoint.equipPointName.Contains("Left"))
                        {
                            var scale = equipedObject.transform.localScale;
                            scale.x *= -1;
                            equipedObject.transform.localScale = scale;
                        }

                        equipPoint.equipmentReference.item = item;
                        equipPoint.equipmentReference.equipedObject = equipedObject;
                        var _equipments = equipPoint.equipmentReference.equipedObject.GetComponents<vIEquipment>();
                        for (int i = 0; i < _equipments.Length; i++)
                        {
                            if (_equipments[i] != null)
                            {
                                _equipments[i].OnEquip(equipPoint.equipmentReference.item);
                                _equipments[i].equipPoint = equipPoint;
                            }
                        }
                        equipPoint.onInstantiateEquiment.Invoke(equipedObject);
                    }
                    else if (equipPoint.equipmentReference != null && equipPoint.equipmentReference.equipedObject != null)
                    {
                        var _equipments = equipPoint.equipmentReference.equipedObject.GetComponents<vIEquipment>();
                        for (int i = 0; i < _equipments.Length; i++)
                        {
                            if (_equipments[i] != null)
                            {
                                _equipments[i].OnUnequip(equipPoint.equipmentReference.item);
                                _equipments[i].equipPoint = null;
                            }
                        }
                        Destroy(equipPoint.equipmentReference.equipedObject);
                        equipPoint.equipmentReference.item = null;
                    }
                }
                inEquip = false;
                inventory.canEquip = true;
                if (equipPoint != null)
                    CheckTwoHandItem(equipPoint, item);
            }
            LockInventoryInput(false);
        }

        protected virtual void CheckTwoHandItem(EquipPoint equipPoint, vItem item)
        {
            if (item == null) return;
            var opposite = equipPoints.Find(ePoint => ePoint.area != null && ePoint.equipPointName.Equals("LeftArm") && ePoint.area.currentEquipedItem != null);
            if (equipPoint.equipPointName.Equals("LeftArm"))
                opposite = equipPoints.Find(ePoint => ePoint.area != null && ePoint.equipPointName.Equals("RightArm") && ePoint.area.currentEquipedItem != null);
            else if (!equipPoint.equipPointName.Equals("RightArm"))
            {
                return;
            }
            if (opposite != null && (item.twoHandWeapon || opposite.area.currentEquipedItem.twoHandWeapon))
            {
                opposite.area.RemoveCurrentItem();
            }
        }

        IEnumerator UnequipItemRoutine(EquipPoint equipPoint, vItem item)
        {
            LockInventoryInput(true);
            if (!inEquip)
            {
                inventory.canEquip = false;
                inEquip = true;
                if (equipPoint != null && equipPoint.equipmentReference != null && equipPoint.equipmentReference.equipedObject != null)
                {
                    var _equipments = equipPoint.equipmentReference.equipedObject.GetComponents<vIEquipment>();
                    for (int i = 0; i < _equipments.Length; i++)
                    {
                        if (_equipments[i] != null)
                        {
                            _equipments[i].OnUnequip(equipPoint.equipmentReference.item);
                            _equipments[i].equipPoint = null;
                        }
                    }
                    if (!inventory.isOpen)
                    {
                        while (equipTimer > 0 && !string.IsNullOrEmpty(item.DisableAnim))
                        {
                            equipTimer -= Time.deltaTime;
                            yield return new WaitForEndOfFrame();
                        }
                    }
                    Destroy(equipPoint.equipmentReference.equipedObject);
                    equipPoint.equipmentReference.item = null;
                }
                inEquip = false;
                inventory.canEquip = true;
            }
            LockInventoryInput(false);
        }

        protected virtual void OnOpenCloseInventory(bool value)
        {
            if (value)
                animator.SetTrigger("ResetState");

            onOpenCloseInventory.Invoke(value);
        }

        /// <summary>
        /// Equip item to specific area and specific slot
        /// </summary>
        /// <param name="indexOfArea">Index of Equip Area</param>
        /// <param name="indexOfSlot">Index of Slot in Equip Area</param>
        /// <param name="item">Item to Equip</param>
        /// <param name="immediate">Force immediate</param>
        public virtual void EquipItemToEquipArea(int indexOfArea, int indexOfSlot, vItem item, bool immediate = false)
        {
            if (!inventory) return;
            if (immediate)
                inventory.isOpen = immediate;
            if (inventory.equipAreas != null && indexOfArea < inventory.equipAreas.Length)
            {
                var area = inventory.equipAreas[indexOfArea];
                if (area != null)
                {
                    area.AddItemToEquipSlot(indexOfSlot, item);
                }
            }
            if (immediate)
                inventory.isOpen = false;
        }

        /// <summary>
        /// Unequip item of specific area and specific slot
        /// </summary>
        /// <param name="indexOfArea">Index of Equip Area</param>
        /// <param name="indexOfSlot">Index of Slot in Equip Area</param>
        /// <param name="immediate">For to unequip immediate</param>
        public virtual void UnequipItemOfEquipArea(int indexOfArea, int indexOfSlot, bool immediate = false)
        {
            if (!inventory) return;
            if (immediate)
                inventory.isOpen = immediate;
            if (inventory.equipAreas != null && indexOfArea < inventory.equipAreas.Length)
            {
                var area = inventory.equipAreas[indexOfArea];
                if (area != null)
                {
                    area.RemoveItemOfEquipSlot(indexOfSlot);
                }
            }
            if (immediate)
                inventory.isOpen = false;
        }

        /// <summary>
        /// Equip or change Item to current equiped slot of area by equipPointName
        /// </summary>
        /// <param name="item">Item to equip</param>
        /// <param name="indexOfArea">Index of Equip area</param>
        /// <param name="immediate">Force equip Immediate</param>
        public virtual void EquipCurrentItemToArea(vItem item, int indexOfArea, bool immediate = false)
        {
            if (!inventory && items.Count == 0) return;

            if (immediate)
                inventory.isOpen = immediate;
            if (inventory.equipAreas != null && indexOfArea < inventory.equipAreas.Length)
            {
                inventory.equipAreas[indexOfArea].AddCurrentItem(item);
            }
            if (immediate)
                inventory.isOpen = false;
        }

        /// <summary>
        /// Unequip current equiped item of specific area 
        /// </summary>
        /// <param name="indexOfArea">Index of Equip area</param>
        /// <param name="immediate">Force unequip Immediate</param>
        public virtual void UnequipCurrentEquipedItem(int indexOfArea, bool immediate = false)
        {
            if (!inventory && items.Count == 0) return;

            if (immediate)
                inventory.isOpen = immediate;
            if (inventory.equipAreas != null && indexOfArea < inventory.equipAreas.Length)
            {
                inventory.equipAreas[indexOfArea].RemoveCurrentItem();
            }
            if (immediate)
                inventory.isOpen = false;
        }

        /// <summary>
        /// Drop current equiped item of specific area
        /// </summary>
        /// <param name="indexOfArea">Index of Equip Area</param>
        /// <param name="immediate">Force to Drop immediate</param>
        public virtual void DropCurrentEquipedItem(int indexOfArea, bool immediate = false)
        {
            if (!inventory && items.Count == 0) return;

            if (immediate)
                inventory.isOpen = immediate;
            if (inventory.equipAreas != null && indexOfArea < inventory.equipAreas.Length)
            {
                var item = inventory.equipAreas[indexOfArea].currentEquipedItem;
                if (item)
                    DropItem(item, item.amount);
            }

            if (immediate)
                inventory.isOpen = false;
        }

        /// <summary>
        /// Leave (Destroy) current equiped item of specific area
        /// </summary>
        /// <param name="indexOfArea">Index of Equip Area</param>
        /// <param name="immediate">Force to Leave immediate</param>
        public virtual void LeaveCurrentEquipedItem(int indexOfArea, bool immediate = false)
        {
            if (!inventory && items.Count == 0) return;

            if (immediate)
                inventory.isOpen = immediate;
            if (inventory.equipAreas != null && indexOfArea < inventory.equipAreas.Length)
            {
                var item = inventory.equipAreas[indexOfArea].currentEquipedItem;
                if (item)
                    DestroyItem(item, item.amount);
            }

            if (immediate)
                inventory.isOpen = false;
        }

        /// <summary>
        /// Auto equip Item
        /// Ps: If item type doesn't match to any area or slot 
        /// </summary>
        /// <param name="item">Item to equip</param>
        /// <param name="immediate">Force equip immediate</param>
        public virtual void AutoEquipItem(vItem item, int indexArea, bool immediate = false)
        {
            if (!inventory) return;
            if (immediate)
                inventory.isOpen = immediate;
            if (inventory.equipAreas != null && inventory.equipAreas.Length > 0 && indexArea < inventory.equipAreas.Length)
            {
                var validSlot = inventory.equipAreas[indexArea].equipSlots.Find(slot => slot.isValid && slot.item == null && slot.itemType.Contains(item.type));
                if (validSlot && !inventory.equipAreas[indexArea].equipSlots.Exists(slot => slot.item == item))
                {
                    var indexOfSlot = inventory.equipAreas[indexArea].equipSlots.IndexOf(validSlot);
                    if (validSlot.item != item)
                        EquipItemToEquipArea(indexArea, indexOfSlot, item);
                }
            }
            else
            {
                Debug.LogWarning("Fail to auto equip " + item.name + " on equipArea " + indexArea);
            }

            if (immediate)
                inventory.isOpen = false;
        }

        #region Item Collector    

        public virtual void CollectItems(List<ItemReference> collection, bool immediate = false)
        {
            foreach (ItemReference reference in collection)
            {
                AddItem(reference, immediate);
            }
        }

        public virtual void CollectItem(ItemReference itemRef, bool immediate = false)
        {

            AddItem(itemRef, immediate);
        }

        public virtual void OnReceiveAction(vTriggerGenericAction action)
        {
            var collection = action.GetComponentInChildren<vItemCollection>();
            if (collection != null)
            {
                if (collection.items.Count > 0)
                {
                    var itemCol = collection.items.vCopy();
                    StartCoroutine(CollectItemsWithDelay(itemCol, collection.onCollectDelay, collection.textDelay, collection.immediate));
                }
            }
        }

        public virtual IEnumerator CollectItemsWithDelay(List<ItemReference> collection, float onCollectDelay, float textDelay, bool immediate)
        {
            yield return new WaitForSeconds(onCollectDelay);

            for (int i = 0; i < collection.Count; i++)
            {
                yield return new WaitForSeconds(textDelay);

                var item = itemListData.items.Find(_item => _item.id == collection[i].id);
                if (item != null && vItemCollectionDisplay.Instance != null)
                {
                    vItemCollectionDisplay.Instance.FadeText("Acquired:" + " " + collection[i].amount + " " + item.name, 4, 0.25f);
                }
                CollectItem(collection[i], immediate);
            }
        }

        public virtual IEnumerator CollectItemWithDelay(ItemReference itemRef, float onCollectDelay, float textDelay, bool immediate)
        {
            yield return new WaitForSeconds(onCollectDelay + textDelay);

            var item = itemListData.items.Find(_item => _item.id == itemRef.id);
            if (item != null && vItemCollectionDisplay.Instance != null)
            {
                vItemCollectionDisplay.Instance.FadeText("Acquired:" + " " + itemRef.amount + " " + item.name, 4, 0.25f);
            }
            CollectItem(itemRef, immediate);
        }

        #endregion
    }


    [System.Serializable]
    public class ItemReference
    {
        public int id;
        public int amount;
        public ItemReference(int id)
        {
            this.id = id;
            this.autoEquip = false;
        }
        public List<vItemAttribute> attributes;
        public bool changeAttributes;

        public bool autoEquip = false;
        public int indexArea;
    }

    [System.Serializable]
    public class EquipPoint
    {
        #region SeralizedProperties in CustomEditor

        [SerializeField]
        public string equipPointName;
        public EquipmentReference equipmentReference = new EquipmentReference();
        [HideInInspector]
        public vEquipArea area;
        public vHandler handler = new vHandler();
        //public Transform defaultPoint;
        //public List<Transform> customPoints = new List<Transform>();
        public OnInstantiateItemObjectEvent onInstantiateEquiment = new OnInstantiateItemObjectEvent();

        #endregion
    }

    public class EquipmentReference
    {
        public GameObject equipedObject;
        public vItem item;
    }

    [System.Serializable]
    public class ApplyAttributeEvent
    {
        [SerializeField]
        public vItemAttributes attribute;
        [SerializeField]
        public OnApplyAttribute onApplyAttribute;
    }

}

