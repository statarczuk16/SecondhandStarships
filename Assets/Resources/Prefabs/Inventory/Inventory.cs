using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------
// Enums
// ---------------------------------------------------------------------

public enum Tier
{
    Tier1,
    Tier2,
    Tier3
}

// Materials and Parts are identical from a storage perspective — both are
// just "type + tier, stacked by count." They share one ID space here.
// (Crafting logic upstream still treats them differently; that's a recipe
// concern, not an inventory concern.)
public enum ItemType
{
    // --- Materials ---
    Wire,
    Insulation,
    Pipe,
    Gasket,
    Tubing,
    Switch,
    SheetMetal,
    Rod,
    Semiconductor,
    Capacitor,
    Mesh,
    Magnet,
    Lens,
    Lubricant,
    Gear,
    Canister,
    Ceramic,
    Acid,

    // --- Parts ---
    Motor,
    Coil,
    HeatingElement,
    Microcontroller,
    SensorModule,
    JunctionBox,
    Pump,
    Filter,
    Actuator,
    Fan,
    Gimbal,
    Igniter
}

public enum InventorySlotType
{
    Empty,
    Item,
    Module
}

// ---------------------------------------------------------------------
// Data
// ---------------------------------------------------------------------

// A single grid cell. This IS the persistent storage unit — unlike the old
// auto-packed-list version, a slot's index never shifts when other slots
// change. Removing slot 3 leaves slot 3 empty; it doesn't collapse slot 4
// into its place. That's what makes this Diablo/Subnautica-style rather
// than a flat inventory list rendered as a grid.
[System.Serializable]
public class Data_InventorySlot
{
    public InventorySlotType slot_type = InventorySlotType.Empty;

    // Valid when slot_type == Item
    public ItemType item_type;
    public Tier tier;
    public int count;

    // Valid when slot_type == Module
    public Data_ShipModule module;

    public bool IsEmpty => slot_type == InventorySlotType.Empty;

    public void Clear()
    {
        slot_type = InventorySlotType.Empty;
        item_type = default;
        tier = default;
        count = 0;
        module = null;
    }
}

// ---------------------------------------------------------------------
// Inventory
// ---------------------------------------------------------------------

[System.Serializable]
public class Inventory
{
    [Header("Capacity")]
    [SerializeField] private int max_slots = 30;

    [Header("Slots")]
    [SerializeField] private List<Data_InventorySlot> slots = new List<Data_InventorySlot>();

    public Inventory()
    {
        EnsureCapacity();
    }

    public Inventory(int maxSlots)
    {
        max_slots = Mathf.Max(0, maxSlots);
        EnsureCapacity();
    }

    public int MaxSlots => max_slots;

    public int UsedSlots
    {
        get
        {
            EnsureCapacity();
            int used = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty) used++;
            }
            return used;
        }
    }

    public int FreeSlots => MaxSlots - UsedSlots;
    public bool IsFull => FreeSlots <= 0;

    // Pads the slot list up to max_slots. Never shrinks here — shrinking is
    // an explicit, validated operation (TrySetMaxSlots) since it can destroy
    // items if done carelessly.
    private void EnsureCapacity()
    {
        if (slots == null)
        {
            slots = new List<Data_InventorySlot>();
        }

        while (slots.Count < max_slots)
        {
            slots.Add(new Data_InventorySlot());
        }
    }

    public Data_InventorySlot GetSlot(int index)
    {
        EnsureCapacity();
        if (index < 0 || index >= slots.Count) return null;
        return slots[index];
    }

    public IReadOnlyList<Data_InventorySlot> Slots
    {
        get
        {
            EnsureCapacity();
            return slots;
        }
    }

    /// <summary>
    /// Grows immediately. Shrinking only succeeds if nothing occupied would
    /// be truncated off the end — refuses to silently destroy items.
    /// </summary>
    public bool TrySetMaxSlots(int newMax, out string error)
    {
        EnsureCapacity();

        if (newMax < 0)
        {
            error = "max slots cannot be negative";
            return false;
        }

        if (newMax < max_slots)
        {
            for (int i = newMax; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    error = $"cannot shrink below slot {i}, which is occupied";
                    return false;
                }
            }
            slots.RemoveRange(newMax, slots.Count - newMax);
        }

        max_slots = newMax;
        EnsureCapacity();
        error = null;
        return true;
    }

    // -------------------------------------------------------------
    // Items (materials & parts)
    // -------------------------------------------------------------

    public bool TryAddItem(ItemType item_type, Tier tier, int amount, out string error)
    {
        return TryAddItem(item_type, tier, amount, -1, out error);
    }

    /// <summary>
    /// If preferSlotIndex is a valid empty slot or a matching stack, the
    /// item lands there first. Otherwise falls back to an existing matching
    /// stack anywhere, then the first empty slot.
    /// </summary>
    public bool TryAddItem(ItemType item_type, Tier tier, int amount, int preferSlotIndex, out string error)
    {
        EnsureCapacity();

        if (amount <= 0)
        {
            error = "amount must be positive";
            return false;
        }

        if (preferSlotIndex >= 0 && preferSlotIndex < slots.Count)
        {
            Data_InventorySlot preferred = slots[preferSlotIndex];
            bool matchesPreferred = preferred.slot_type == InventorySlotType.Item
                && preferred.item_type == item_type
                && preferred.tier == tier;

            if (preferred.IsEmpty || matchesPreferred)
            {
                preferred.slot_type = InventorySlotType.Item;
                preferred.item_type = item_type;
                preferred.tier = tier;
                preferred.count += amount;
                error = null;
                TopicLogger.Log(LogTopic.Inventory, LogLevel.DEBUG,
                    $"Placed {amount}x {item_type} (T{(int)tier + 1}) in slot {preferSlotIndex}");
                return true;
            }
        }

        int existingIndex = FindItemSlotIndex(item_type, tier);
        if (existingIndex >= 0)
        {
            slots[existingIndex].count += amount;
            error = null;
            TopicLogger.Log(LogTopic.Inventory, LogLevel.DEBUG,
                $"Stacked {amount}x {item_type} (T{(int)tier + 1}) -> now {slots[existingIndex].count}");
            return true;
        }

        int emptyIndex = FindEmptySlotIndex();
        if (emptyIndex < 0)
        {
            error = "inventory full";
            TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN, $"Failed to add {item_type}: inventory full");
            return false;
        }

        Data_InventorySlot slot = slots[emptyIndex];
        slot.slot_type = InventorySlotType.Item;
        slot.item_type = item_type;
        slot.tier = tier;
        slot.count = amount;

        error = null;
        TopicLogger.Log(LogTopic.Inventory, LogLevel.DEBUG,
            $"New stack: {item_type} (T{(int)tier + 1}) x{amount} in slot {emptyIndex}");
        return true;
    }

    /// <summary>
    /// Removes amount of a given item, pulling from whichever matching
    /// stacks it finds (may span multiple slots). Slots that empty out are
    /// cleared in place — they do not disappear or shift other slots.
    /// </summary>
    public bool TryRemoveItem(ItemType item_type, Tier tier, int amount, out string error)
    {
        EnsureCapacity();

        if (amount <= 0)
        {
            error = "amount must be positive";
            return false;
        }

        if (GetItemCount(item_type, tier) < amount)
        {
            error = "insufficient quantity";
            return false;
        }

        int remaining = amount;
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            Data_InventorySlot slot = slots[i];
            if (slot.slot_type != InventorySlotType.Item || slot.item_type != item_type || slot.tier != tier)
            {
                continue;
            }

            int take = Mathf.Min(remaining, slot.count);
            slot.count -= take;
            remaining -= take;

            if (slot.count <= 0)
            {
                slot.Clear();
            }
        }

        error = null;
        return true;
    }

    /// <summary>Removes amount from one specific slot — the direct UI operation (split stack, drop N).</summary>
    public bool TryRemoveItemFromSlot(int slotIndex, int amount, out string error)
    {
        EnsureCapacity();

        if (slotIndex < 0 || slotIndex >= slots.Count)
        {
            error = "invalid slot index";
            return false;
        }

        Data_InventorySlot slot = slots[slotIndex];
        if (slot.slot_type != InventorySlotType.Item)
        {
            error = "slot does not contain an item";
            return false;
        }

        if (amount <= 0)
        {
            error = "amount must be positive";
            return false;
        }

        if (slot.count < amount)
        {
            error = "insufficient quantity";
            return false;
        }

        slot.count -= amount;
        if (slot.count <= 0)
        {
            slot.Clear();
        }

        error = null;
        return true;
    }

    public int GetItemCount(ItemType item_type, Tier tier)
    {
        EnsureCapacity();
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            Data_InventorySlot slot = slots[i];
            if (slot.slot_type == InventorySlotType.Item && slot.item_type == item_type && slot.tier == tier)
            {
                total += slot.count;
            }
        }
        return total;
    }

    public bool HasItem(ItemType item_type, Tier tier, int amount)
    {
        return GetItemCount(item_type, tier) >= amount;
    }

    private int FindItemSlotIndex(ItemType item_type, Tier tier)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Data_InventorySlot slot = slots[i];
            if (slot.slot_type == InventorySlotType.Item && slot.item_type == item_type && slot.tier == tier)
            {
                return i;
            }
        }
        return -1;
    }

    private int FindEmptySlotIndex()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty) return i;
        }
        return -1;
    }

    // -------------------------------------------------------------
    // Modules
    // -------------------------------------------------------------

    public bool TryAddModule(Data_ShipModule module, out string error)
    {
        return TryAddModule(module, -1, out error);
    }

    public bool TryAddModule(Data_ShipModule module, int preferSlotIndex, out string error)
    {
        EnsureCapacity();

        if (module == null)
        {
            error = "module is null";
            return false;
        }

        if (preferSlotIndex >= 0 && preferSlotIndex < slots.Count && slots[preferSlotIndex].IsEmpty)
        {
            slots[preferSlotIndex].slot_type = InventorySlotType.Module;
            slots[preferSlotIndex].module = module;
            error = null;
            TopicLogger.Log(LogTopic.Inventory, LogLevel.DEBUG, $"Placed module {module.part_name} in slot {preferSlotIndex}");
            return true;
        }

        int emptyIndex = FindEmptySlotIndex();
        if (emptyIndex < 0)
        {
            error = "inventory full";
            TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN, $"Failed to add module {module.part_name}: inventory full");
            return false;
        }

        slots[emptyIndex].slot_type = InventorySlotType.Module;
        slots[emptyIndex].module = module;
        error = null;
        TopicLogger.Log(LogTopic.Inventory, LogLevel.DEBUG, $"Added module {module.part_name} to slot {emptyIndex}");
        return true;
    }

    public bool TryRemoveModule(Data_ShipModule module)
    {
        EnsureCapacity();
        int index = FindModuleSlotIndex(module);
        if (index < 0) return false;

        slots[index].Clear();
        TopicLogger.Log(LogTopic.Inventory, LogLevel.DEBUG, $"Removed module {module.part_name}");
        return true;
    }

    public bool ContainsModule(Data_ShipModule module)
    {
        return FindModuleSlotIndex(module) >= 0;
    }

    public void ClearModules()
    {
        EnsureCapacity();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slot_type == InventorySlotType.Module)
            {
                slots[i].Clear();
            }
        }
    }

    private int FindModuleSlotIndex(Data_ShipModule module)
    {
        if (module == null) return -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slot_type == InventorySlotType.Module && slots[i].module == module)
            {
                return i;
            }
        }
        return -1;
    }

    // -------------------------------------------------------------
    // Slot manipulation (drag/drop, click-to-place UI)
    // -------------------------------------------------------------

    /// <summary>
    /// Moves whatever occupies fromIndex to toIndex. Merges into a matching
    /// item stack if the target holds one; otherwise swaps the two slots'
    /// contents. This is the one primitive UI needs for drag/drop or
    /// pick-up-and-place interactions.
    /// </summary>
    public bool TryMoveSlot(int fromIndex, int toIndex, out string error)
    {
        EnsureCapacity();

        if (fromIndex < 0 || fromIndex >= slots.Count || toIndex < 0 || toIndex >= slots.Count)
        {
            error = "invalid slot index";
            return false;
        }

        if (fromIndex == toIndex)
        {
            error = null;
            return true;
        }

        Data_InventorySlot from = slots[fromIndex];
        Data_InventorySlot to = slots[toIndex];

        if (from.IsEmpty)
        {
            error = "source slot is empty";
            return false;
        }

        if (to.IsEmpty)
        {
            slots[toIndex] = from;
            slots[fromIndex] = new Data_InventorySlot();
            error = null;
            return true;
        }

        bool bothMatchingItems = from.slot_type == InventorySlotType.Item
            && to.slot_type == InventorySlotType.Item
            && from.item_type == to.item_type
            && from.tier == to.tier;

        if (bothMatchingItems)
        {
            to.count += from.count;
            from.Clear();
            error = null;
            return true;
        }

        // Different contents occupy the target — swap positions.
        slots[fromIndex] = to;
        slots[toIndex] = from;
        error = null;
        return true;
    }

    // -------------------------------------------------------------
    // Compact views — for code that just wants "all the modules"/"all the
    // stacks" and doesn't care which physical slot they live in (recipe
    // checks, a scroll-through carousel). Order is NOT guaranteed stable
    // across calls if slot contents change; don't use this to track "the
    // 3rd item" persistently — use slot indices for that.
    // -------------------------------------------------------------

    public IReadOnlyList<Data_ShipModule> GetModulesCompact()
    {
        EnsureCapacity();
        var result = new List<Data_ShipModule>();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slot_type == InventorySlotType.Module)
            {
                result.Add(slots[i].module);
            }
        }
        return result;
    }
}