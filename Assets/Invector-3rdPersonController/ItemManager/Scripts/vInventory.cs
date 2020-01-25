using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;
namespace Invector.vItemManager
{
    using vCharacterController;
    [vClassHeader("vInventory")]
    public class vInventory : vMonoBehaviour
    {
        #region Item Variables

        public delegate List<vItem> GetItemsDelegate();
        public delegate bool LockInventoryInputEvent();

        public GetItemsDelegate GetItemsHandler;
        public GetItemsDelegate GetItemsAllHandler;
        public LockInventoryInputEvent IsLockedEvent;

        [vEditorToolbar("Settings")]
        public vInventoryWindow firstWindow;
        [Range(0, 1)]
        public float timeScaleWhileIsOpen = 0;
        public float originalTimeScale = 1f;
        public bool updatedTimeScale;
        public bool dontDestroyOnLoad = true;
        public List<ChangeEquipmentControl> changeEquipmentControllers;
        [HideInInspector]
        public List<vInventoryWindow> windows = new List<vInventoryWindow>();
        [HideInInspector]
        public vInventoryWindow currentWindow;
        [vEditorToolbar("Input Mapping")]
        public GenericInput openInventory = new GenericInput("I", "Start", "Start");
        public GenericInput openCurrentSlot = new GenericInput("O", "A", "A");
        public GenericInput removeEquipment = new GenericInput("Backspace", "X", "X");
        [Header("This fields will override the EventSystem Input")]
        public GenericInput horizontal = new GenericInput("Horizontal", "D-Pad Horizontal", "Horizontal");
        public GenericInput vertical = new GenericInput("Vertical", "D-Pad Vertical", "Vertical");
        public GenericInput submit = new GenericInput("Return", "A", "A");
        public GenericInput cancel = new GenericInput("Backspace", "B", "B");        
       
        [vEditorToolbar("Events")]     
        public OnOpenCloseInventory onOpenCloseInventory;
        public OnUpdateItemUsageTime onUpdateItemUsageTime;
        public OnHandleItemEvent onUseItem; 
        public OnChangeItemAmount onLeaveItem, onDropItem;
     
        public OnChangeEquipmentEvent onEquipItem, onUnequipItem;     
        [HideInInspector]
        public bool isOpen, canEquip,lockInventoryInput;
        [HideInInspector]
        public vEquipArea[] equipAreas;

        public List<vItem> items
        {
            get
            {
                if (GetItemsHandler != null) return GetItemsHandler();
                return new List<vItem>();
            }
        }

        public List<vItem> allItems
        {
            get
            {
                if (GetItemsAllHandler != null) return GetItemsAllHandler();
                return new List<vItem>();
            }
        }

        private vEquipArea currentEquipArea;
        private StandaloneInputModule inputModule;        
        #endregion

        void Start()
        {
            canEquip = true;
            inputModule = FindObjectOfType<StandaloneInputModule>();
            if(inputModule == null)
            {
                inputModule = (new GameObject("EventSystem")).AddComponent<StandaloneInputModule>();
            }
            equipAreas = GetComponentsInChildren<vEquipArea>(true);
            foreach (vEquipArea equipArea in equipAreas)
            {
                equipArea.Init();
                equipArea.onEquipItem.AddListener(EquipItem);
                equipArea.onUnequipItem.AddListener(UnequipItem);
                equipArea.onSelectEquipArea.AddListener(SelectArea);
            }
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
            if(vGameController.instance)
                vGameController.instance.OnReloadGame.AddListener(OnReloadGame);
        }

        public void OnReloadGame()
        {
            StartCoroutine(ReloadEquipment());
        }

        IEnumerator ReloadEquipment()
        {
            yield return new WaitForEndOfFrame();
            inputModule = FindObjectOfType<StandaloneInputModule>();
            isOpen = true;
            foreach (ChangeEquipmentControl changeEquip in changeEquipmentControllers)
            {
                if (changeEquip.equipArea != null)
                {
                    foreach (vEquipSlot slot in changeEquip.equipArea.equipSlots)
                    {
                        //if (slot.item != null && items.Find(i => i.id == slot.item.id) != null)
                        //{
                        //    EquipItem(changeEquip.equipArea, slot.item);
                        //}
                        //else 
                        if (changeEquip.equipArea.currentEquipedItem == null)
                        {
                            UnequipItem(changeEquip.equipArea, slot.item);
                            changeEquip.equipArea.UnequipItem(slot);
                        }
                        else
                        {
                            changeEquip.equipArea.UnequipItem(slot);
                        }
                    }
                }
            }
            isOpen = false;
        }

        public bool IsLocked()
        {
            var _locked = (IsLockedEvent != null ? IsLockedEvent.Invoke() : false);
            return _locked || lockInventoryInput;
        }

        void LateUpdate()
        {
            if (IsLocked()) return;

            ControlWindowsInput();

            if (!isOpen)
                ChangeEquipmentInput();
            else
            {
                UpdateEventSystemInput();
                RemoveEquipmentInput();
                OpenCurrentSlot();
            }
            if(Input.GetKeyDown(KeyCode.P))
            {
                CloseInventory();
            }
        }

        public virtual void ControlWindowsInput()
        {
            // enable first window
            if ((windows.Count == 0 || windows[windows.Count - 1] == firstWindow))
            {
                if (!firstWindow.gameObject.activeSelf && openInventory.GetButtonDown() && canEquip)
                {                  
                    OpenInventory();
                    return;
                }

                else if (firstWindow.gameObject.activeSelf && (openInventory.GetButtonDown() || cancel.GetButtonDown()))
                {                   
                    CloseInventory();
                    return;
                }
            }
            if (!isOpen) return;
            // disable current window
            if(openInventory.GetButtonDown())
            {              
                CloseInventory();
                return;
            }
            if ((windows.Count > 0 && windows[windows.Count - 1] != firstWindow) && cancel.GetButtonDown())
            {
                currentEquipArea = null;
                if (windows[windows.Count - 1].ContainsPop_up())
                {
                    windows[windows.Count - 1].RemoveLastPop_up();
                    return;
                }
                else
                {
                    windows[windows.Count - 1].gameObject.SetActive(false);
                    windows.RemoveAt(windows.Count - 1);// remove last window of the window list
                    if (windows.Count > 0)
                    {
                        windows[windows.Count - 1].gameObject.SetActive(true);
                        currentWindow = windows[windows.Count - 1];
                    }
                    else
                        currentWindow = null; // clear currentWindow if  window list count == 0        
                }
            }
            // check if currenWindow  that was closed
            if (currentWindow != null && !currentWindow.gameObject.activeSelf)
            {
                // remove currentWindow of the window list
                if (windows.Contains(currentWindow))
                    windows.Remove(currentWindow);
                // set currentWindow if window list have more windows
                if (windows.Count > 0)
                {
                    windows[windows.Count - 1].gameObject.SetActive(true);
                    currentWindow = windows[windows.Count - 1];
                }
                else
                    currentWindow = null;// clear currentWindow if  window list count == 0        
            }
        }

        public virtual void OpenInventory()
        {
            if (isOpen) return;
            firstWindow.gameObject.SetActive(true);
            isOpen = true;
            onOpenCloseInventory.Invoke(true);

            if (!updatedTimeScale)
            {
                updatedTimeScale = true;
                originalTimeScale = Time.timeScale;
            }
            Time.timeScale = timeScaleWhileIsOpen;
        }

        public virtual void CloseInventory()
        {
            if(isOpen)
            {
               if(! firstWindow.gameObject.activeSelf)
                {
                    for (int i = windows.Count - 1; i > 0; i--)
                    {
                        if (windows[i].ContainsPop_up())
                            windows[i].RemoveLastPop_up();
                        windows[i].gameObject.SetActive(false);

                    }
                }
            
                windows.Clear();
                currentEquipArea = null;
                currentWindow = null;
                firstWindow.gameObject.SetActive(false);
                isOpen = false;
                onOpenCloseInventory.Invoke(false);
                Time.timeScale = originalTimeScale;
                if (updatedTimeScale)
                    updatedTimeScale = false;
            }
         
        }
        /// <summary>
        /// Input Button to remove equiped Item
        /// </summary>
        void RemoveEquipmentInput()
        {            
            if (currentEquipArea != null && removeEquipment.GetButtonDown())
            {
                currentEquipArea.UnequipCurrentItem();
            }
        }

        /// <summary>
        /// Input Button to open the selected equipSlot
        /// </summary>
        void OpenCurrentSlot()
        {
            if (vInput.instance.inputDevice == InputDevice.Mobile)
            {
                if (currentEquipArea != null && openCurrentSlot.GetButtonDown())
                {
                    if (currentEquipArea.currentSelectedSlot != null)
                        currentEquipArea.OnSubmitSlot(currentEquipArea.currentSelectedSlot);
                }
            }
        }

        void SelectArea(vEquipArea equipArea)
        {
            currentEquipArea = equipArea;
        }

        /// <summary>
        /// Input for change equiped Item
        /// </summary>
        void ChangeEquipmentInput()
        {
            // display equiped itens
            if (changeEquipmentControllers.Count > 0 && canEquip)
            {
                foreach (ChangeEquipmentControl changeEquip in changeEquipmentControllers)
                {
                    UseItemInput(changeEquip);
                    if (changeEquip.equipArea != null)
                    {
                        if (vInput.instance.inputDevice == InputDevice.MouseKeyboard|| vInput.instance.inputDevice == InputDevice.Mobile)
                        {
                            if (changeEquip.previousItemInput.GetButtonDown())
                                changeEquip.equipArea.PreviousEquipSlot();
                            if (changeEquip.nextItemInput.GetButtonDown())
                                changeEquip.equipArea.NextEquipSlot();
                        }
                        else if (vInput.instance.inputDevice == InputDevice.Joystick)
                        {
                            if (changeEquip.previousItemInput.GetAxisButtonDown(-1))
                                changeEquip.equipArea.PreviousEquipSlot();
                            if (changeEquip.nextItemInput.GetAxisButtonDown(1))
                                changeEquip.equipArea.NextEquipSlot();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if Equipment in EquipArea is change while drop,leave or use item
        /// </summary>
        public void CheckEquipmentChanges()
        {
            foreach (ChangeEquipmentControl changeEquip in changeEquipmentControllers)
            {
                foreach (vEquipSlot slot in changeEquip.equipArea.equipSlots)
                {
                    if (slot != null)
                    {
                        if (slot.item != null && !items.Contains(slot.item))
                        {
                            changeEquip.equipArea.UnequipItem(slot);
                            if(changeEquip.display)
                                changeEquip.display.RemoveItem();
                        }
                    }
                }
            }
        }
        
        void UpdateEventSystemInput()
        {
            if (inputModule)
            {
                inputModule.horizontalAxis = horizontal.buttonName;
                inputModule.verticalAxis = vertical.buttonName;
                inputModule.submitButton = submit.buttonName;
                inputModule.cancelButton = cancel.buttonName;
            }
            else
            {
                inputModule = FindObjectOfType<StandaloneInputModule>();
            }
        }

        void UseItemInput(ChangeEquipmentControl changeEquip)
        {
            if (changeEquip.display != null && changeEquip.display.item != null && changeEquip.display.item.type == vItemType.Consumable)
            {
                if (changeEquip.useItemInput.GetButtonDown() && changeEquip.display.item.amount > 0)
                {
                    //changeEquip.display.item.amount--;
                    OnUseItem(changeEquip.display.item);
                }
            }
        }
        
        internal void OnUseItem(vItem item)
        {
            onUseItem.Invoke(item);
        }

        internal void OnUseItemImmediate(vItem item)
        {
            onUseItem.Invoke(item);
        }

        internal void OnLeaveItem(vItem item, int amount)
        {
            onLeaveItem.Invoke(item, amount);
            CheckEquipmentChanges();
        }

        internal void OnDropItem(vItem item, int amount)
        {
            onDropItem.Invoke(item, amount);
            CheckEquipmentChanges();
        }

        /// <summary>
        /// Set current window <see cref="vInventoryWindow"/> call automatically when Enabled
        /// </summary>
        /// <param name="inventoryWindow"></param>  
        internal void SetCurrentWindow(vInventoryWindow inventoryWindow)
        {
            if (!windows.Contains(inventoryWindow))
            {
                windows.Add(inventoryWindow);
                if (currentWindow != null)
                {
                    currentWindow.gameObject.SetActive(false);
                }
                currentWindow = inventoryWindow;
            }
        }

        public void EquipItem(vEquipArea equipArea, vItem item)
        {          
            onEquipItem.Invoke(equipArea, item);
            ChangeEquipmentDisplay(equipArea, item, false);           
        }
       
        public void UnequipItem(vEquipArea equipArea, vItem item)
        {
            onUnequipItem.Invoke(equipArea, item);
            ChangeEquipmentDisplay(equipArea, item);
        }

        void ChangeEquipmentDisplay(vEquipArea equipArea, vItem item, bool removeItem = true)
        {
            if (changeEquipmentControllers.Count > 0)
            {
                var changeEquipControl = changeEquipmentControllers.Find(changeEquip => changeEquip.equipArea != null && changeEquip.equipArea.equipPointName == equipArea.equipPointName && changeEquip.display != null);
                if (changeEquipControl != null)
                {
                    if (removeItem && changeEquipControl.display.item == item)
                    {
                        changeEquipControl.display.RemoveItem();
                        changeEquipControl.display.ItemIdentifier(changeEquipControl.equipArea.indexOfEquipedItem + 1, true);
                    }                        
                    else if (equipArea.currentEquipedItem == item)
                    {
                        changeEquipControl.display.AddItem(item);
                        changeEquipControl.display.ItemIdentifier(changeEquipControl.equipArea.indexOfEquipedItem + 1, true);
                    }
                }
            }
        }
                
    }

    [System.Serializable]
    public class ChangeEquipmentControl
    {
        public GenericInput useItemInput = new GenericInput("U", "Start", "Start");
        public GenericInput previousItemInput = new GenericInput("LeftArrow", "D - Pad Horizontal", "D-Pad Horizontal");
        public GenericInput nextItemInput = new GenericInput("RightArrow", "D - Pad Horizontal", "D-Pad Horizontal");
        public vEquipArea equipArea;
        public vEquipmentDisplay display;
    }

}
