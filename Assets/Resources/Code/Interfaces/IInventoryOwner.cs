// Implemented by any component that owns an Inventory (Controller_Equipment,
// a garage storage container, a ship cargo hold, etc.) so UI like
// Component_InventoryGridUI can display whichever one it's pointed at
// without being coupled to a specific owner type.
public interface IInventoryOwner
{
    Inventory GetInventory();
}