using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Events;

namespace Invector.vItemManager
{
    using vCharacterController;
    public delegate void ItemSlotEvent(vItemSlot item);
    public class vItemSlot : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerEnterHandler, IPointerExitHandler
    {       
        public Image icon;
        public Image blockIcon;
        public Image checkIcon;
        public Text amountText;        
        public vItem item;
        public bool isValid = true;
        [HideInInspector]
        public bool isChecked;        
        public ItemSlotEvent onSubmitSlotCallBack, onSelectSlotCallBack, onDeselectSlotCallBack;
        Color color = Color.white;
        public OnHandleItemEvent onAddItem, onRemoveItem;

        public void Start()
        {
            SetValid(isValid);
            CheckItem(false);
        }

        void LateUpdate()
        {
            if (item != null && this.gameObject.activeSelf)
            {
                if (item.stackable)
                    amountText.text = item.amount.ToString();
                else
                    amountText.text = "";
            }
        }

        public virtual void CheckItem(bool value)
        {
            isChecked = value;
            if (checkIcon)
            {
                checkIcon.gameObject.SetActive(isChecked);
            }
        }

        public virtual void SetValid(bool value)
        {
            isValid = value;
            Selectable sectable = GetComponent<Selectable>();
            if (sectable)
                sectable.interactable = value;
            if (blockIcon == null) return;
            blockIcon.color = value ? Color.clear : Color.white;
            blockIcon.SetAllDirty();
            isValid = value;
        }

        public virtual void AddItem(vItem item)
        {
            if (item != null)
            {
                onAddItem.Invoke(item);
                this.item = item;
                icon.sprite = item.icon;
                color.a = 1;
                icon.color = color;
                if (item.stackable)
                    amountText.text = item.amount.ToString();
                else
                    amountText.text = "";

            }
            else RemoveItem();
            //if(icon)
            //    icon.SetAllDirty();
        }

        public virtual void RemoveItem()
        {
            onRemoveItem.Invoke(item);
            color.a = 0;
            icon.color = color;
            this.item = null;
            amountText.text = "";
            icon.sprite = null;
            icon.SetAllDirty();
        }      

        public virtual bool isOcupad()
        {
            return item != null;
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            if (onSelectSlotCallBack != null)
                onSelectSlotCallBack(this);
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            if (onDeselectSlotCallBack != null)
                onDeselectSlotCallBack(this);
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (isValid)
                if (onSubmitSlotCallBack != null)
                    onSubmitSlotCallBack(this);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if(vInput.instance.inputDevice == InputDevice.MouseKeyboard)
            {
                EventSystem.current.SetSelectedGameObject(this.gameObject);
                if (onSelectSlotCallBack != null)
                    onSelectSlotCallBack(this);
            }            
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (vInput.instance.inputDevice == InputDevice.MouseKeyboard)
            {
                if (onDeselectSlotCallBack != null)
                    onDeselectSlotCallBack(this);
            }
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (vInput.instance.inputDevice == InputDevice.Mobile)
#else
            if (vInput.instance.inputDevice == InputDevice.MouseKeyboard)
#endif
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    if (isValid)
                        if (onSubmitSlotCallBack != null)
                            onSubmitSlotCallBack(this);
                }                   
            }
        }
    }
}

