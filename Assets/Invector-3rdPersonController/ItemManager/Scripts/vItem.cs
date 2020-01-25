using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Invector.vItemManager
{
    [System.Serializable]
    public class vItem : ScriptableObject
    {
        #region SerializedProperties in customEditor
        [HideInInspector]
        public int id;
        [HideInInspector]
        public string description = "Item Description";
        [HideInInspector]
        public vItemType type;
        [HideInInspector]
        public Sprite icon;
        [HideInInspector]
        public bool stackable = true;
        [HideInInspector]
        public int maxStack;
       // [HideInInspector]
        public int amount;
        [HideInInspector]
        public GameObject originalObject;
        [HideInInspector]
        public GameObject dropObject;
        [HideInInspector]
        public List<vItemAttribute> attributes = new List<vItemAttribute>();
        [HideInInspector]
        public bool isInEquipArea;
        #endregion

        #region Properties in defaultInspector
        public bool destroyAfterUse = true;
        public bool canBeUsed = true;
        public bool canBeDroped = true;
        public bool canBeDestroyed = true;
        public bool displayAttributes = true;
      
        //[Header("Usable Settings")]
        //public int UseID;
        //public float useDelayTime = 0.5f;
        [Header("Animation Settings")]
        [vHelpBox("Triggers a animation when Equipping a Weapon or enabling item.\nYou can also trigger an animation if the ItemType is a Consumable")]
        public string EnableAnim = "LowBack";
        [vHelpBox("Triggers a animation when Unequipping a Weapon or disable item")]
        public string DisableAnim = "LowBack";
        [vHelpBox("Delay to enable the Weapon/Item object when Equipping\n If ItemType is a Consumable use this to delay the item usage.")]
        public float enableDelayTime = 0.5f;
        [vHelpBox("Delay to hide the Weapon/Item object when Unequipping")]
        public float disableDelayTime = 0.5f;
        [vHelpBox("If the item is equippable use this to set a custom handler to instantiate the SpawnObject")]
        public string customHandler;
        [vHelpBox("If the item is equippable and need to use two hand\n<color=yellow><b>This option makes it impossible to equip two items</b></color>")]
        public bool twoHandWeapon;
        #endregion
        /// <summary>
        /// Convert Sprite icon to texture
        /// </summary>
        public Texture2D iconTexture
        {
            get
            {
                if (!icon) return null;
                try
                {
                    if (icon.rect.width != icon.texture.width || icon.rect.height != icon.texture.height)
                    {                        
                        Texture2D newText = new Texture2D((int)icon.textureRect.width, (int)icon.textureRect.height);
                        newText.name = icon.name;
                        Color[] newColors = icon.texture.GetPixels((int)icon.textureRect.x, (int)icon.textureRect.y, (int)icon.textureRect.width, (int)icon.textureRect.height);
                        newText.SetPixels(newColors);
                        newText.Apply();
                        return newText;
                    }
                    else
                        return icon.texture;
                }
                catch
                {
                    Debug.LogWarning("Icon texture of the "+name +" is not Readable",icon.texture);
                    return icon.texture;
                }
            }
        }

        public vItemAttribute GetItemAttribute(vItemAttributes attribute)
        {
            if (attributes != null) return attributes.Find(_attribute => _attribute.name == attribute);
            return null;
        }

        public vItemAttribute GetItemAttribute(string name)
        {
            if(attributes!=null)
            return attributes.Find(attribute => attribute.name.ToString().Equals(name));
            return null;
        }
    }
}

