namespace Invector.vItemManager
{
    public interface vIEquipment
    {
        bool isEquiped { get; }
        EquipPoint equipPoint{ get; set; }
        vItem referenceItem { get; }
        void OnEquip(vItem item);
        void OnUnequip(vItem item);
    }
}