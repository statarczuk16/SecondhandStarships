using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drop this on the same GameObject as InventoryGridController (and its
/// PanelRenderer). Does nothing on its own — set the inspector parameters
/// below, then run "Attach To Inventory UI" from the context menu. That one
/// action builds a fresh Inventory from those parameters, seeds it, and
/// hands it to the UI exactly the way the real game would when a panel is
/// given an inventory to display. No auto-wiring, no runtime key bindings.
/// </summary>
[RequireComponent(typeof(InventoryGridController))]
public class Component_InventoryTestDriver : MonoBehaviour, IInventoryOwner
{
    [Header("Inventory Parameters")]
    [SerializeField] private int m_max_slots = 20;

    [Header("Seed Data")]
    [SerializeField] private int m_item_stack_count = 3;
    [SerializeField] private int m_module_count = 2;
    [SerializeField] private Vector2Int m_item_amount_range = new Vector2Int(1, 5);

    private Data_Inventory _mDataInventory;
    private int m_test_module_counter;

    private static readonly string[] kSamplePartNames =
    {
        "Fluid Relay", "Power Relay", "Capacitor Bank", "Breaker Box",
        "Flight Console", "Small Tank", "Big Tank", "Bulkhead"
    };

    public Data_Inventory GetInventory()
    {
        return _mDataInventory;
    }

    // -------------------------------------------------------------
    // One-shot setup — the only thing this driver does automatically is
    // nothing. Everything happens here, on demand, from the Inspector.
    // -------------------------------------------------------------

    [ContextMenu("Attach To Inventory UI")]
    public void AttachToInventoryUI()
    {
        Controller_Equipment controller_eq = FindAnyObjectByType<Controller_Equipment>();
       

        _mDataInventory = new Data_Inventory(m_max_slots);
        SeedTestData();

        controller_eq.DisplayInventory(this);
        TopicLogger.Log(LogTopic.Inventory, LogLevel.INFO,
            $"{name}: built a {m_max_slots}-slot inventory and attached it to the grid UI");
    }

    // -------------------------------------------------------------
    // Manual, inspector-driven controls for poking at the inventory after
    // it's attached. None of these run on their own — call them via the
    // context menu (or your own editor tooling) as needed.
    // -------------------------------------------------------------

    [ContextMenu("Add Random Item")]
    public void AddRandomItem()
    {
        if (!EnsureAttached()) return;

        var itemTypes = (ItemType[])Enum.GetValues(typeof(ItemType));
        ItemType type = itemTypes[UnityEngine.Random.Range(0, itemTypes.Length)];
        Tier tier = (Tier)UnityEngine.Random.Range(0, 3);
        int amount = UnityEngine.Random.Range(m_item_amount_range.x, m_item_amount_range.y + 1);

        if (!_mDataInventory.TryAddItem(type, tier, amount, out string error))
        {
            TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN, $"AddRandomItem failed: {error}");
        }
    }

    [ContextMenu("Add Random Module")]
    public void AddRandomModule()
    {
        if (!EnsureAttached()) return;

        string baseName = kSamplePartNames[UnityEngine.Random.Range(0, kSamplePartNames.Length)];
        m_test_module_counter++;

        var data = new Data_ShipModule
        {
            part_name = $"{baseName} #{m_test_module_counter}",
            prefab = null,
            install_state = default
        };

        if (!_mDataInventory.TryAddModule(data, out string error))
        {
            TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN, $"AddRandomModule failed: {error}");
        }
    }

    [ContextMenu("Remove From Random Filled Slot")]
    public void RemoveFromRandomFilledSlot()
    {
        if (!EnsureAttached()) return;

        List<int> filledIndices = new List<int>();
        for (int i = 0; i < _mDataInventory.MaxSlots; i++)
        {
            Data_InventorySlot slot = _mDataInventory.GetSlot(i);
            if (slot != null && !slot.IsEmpty)
            {
                filledIndices.Add(i);
            }
        }

        if (filledIndices.Count == 0)
        {
            TopicLogger.Log(LogTopic.Inventory, LogLevel.INFO, "Nothing to remove — inventory is empty");
            return;
        }

        int index = filledIndices[UnityEngine.Random.Range(0, filledIndices.Count)];
        RemoveSlotContents(index);
    }

    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        if (!EnsureAttached()) return;

        for (int i = 0; i < _mDataInventory.MaxSlots; i++)
        {
            RemoveSlotContents(i);
        }
        TopicLogger.Log(LogTopic.Inventory, LogLevel.INFO, "Cleared test inventory");
    }

    [ContextMenu("Grow +5 Slots")]
    public void GrowSlots()
    {
        if (!EnsureAttached()) return;

        if (!_mDataInventory.TrySetMaxSlots(_mDataInventory.MaxSlots + 5, out string error))
        {
            TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN, $"GrowSlots failed: {error}");
        }
    }

    [ContextMenu("Shrink -5 Slots")]
    public void ShrinkSlots()
    {
        if (!EnsureAttached()) return;

        if (!_mDataInventory.TrySetMaxSlots(Mathf.Max(0, _mDataInventory.MaxSlots - 5), out string error))
        {
            TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN, $"ShrinkSlots failed: {error}");
        }
    }

    private void SeedTestData()
    {
        for (int i = 0; i < m_item_stack_count; i++)
        {
            AddRandomItem();
        }
        for (int i = 0; i < m_module_count; i++)
        {
            AddRandomModule();
        }
    }

    private void RemoveSlotContents(int index)
    {
        Data_InventorySlot slot = _mDataInventory.GetSlot(index);
        if (slot == null || slot.IsEmpty) return;

        if (slot.slot_type == InventorySlotType.Item)
        {
            _mDataInventory.TryRemoveItemFromSlot(index, slot.count, out string error);
            if (error != null)
            {
                TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN, $"Failed clearing slot {index}: {error}");
            }
        }
        else if (slot.slot_type == InventorySlotType.Module)
        {
            _mDataInventory.TryRemoveModule(slot.module);
        }
    }

    private bool EnsureAttached()
    {
        if (_mDataInventory != null) return true;

        TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN,
            $"{name}: not attached yet — run 'Attach To Inventory UI' first");
        return false;
    }
}