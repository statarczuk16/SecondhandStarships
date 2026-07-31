using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>
/// Renders a persistent inventory grid
/// </summary>
public class InventoryGridController : MonoBehaviour
{
    [Tooltip("Must implement IInventoryOwner. Assign the component that owns the Inventory to display (equipment controller, garage storage, cargo hold, etc.).")]
    [SerializeField] private MonoBehaviour m_inventory_owner_behaviour;

    private IInventoryOwner m_inventory_owner;

    private VisualElement m_viewport;
    private Label m_capacity_label;

    private readonly List<VisualElement> m_slot_elements = new List<VisualElement>();
    private readonly List<string> m_bound_signature = new List<string>();

    /// <summary>Fired whenever a slot is clicked, regardless of whether it triggered a pick-up/place/cancel.</summary>
    public event Action<int> OnSlotClicked;

    // Index of the slot currently "picked up" awaiting a destination, or -1 if nothing held.
    private int m_held_slot_index = -1;
    
    // Lets code repoint this UI at a different inventory at runtime (e.g.
    // switching the grid from the player's backpack to a garage storage
    // container when the player opens it).
    public void SetInventoryOwner(IInventoryOwner owner)
    {
        m_inventory_owner = owner;
        m_held_slot_index = -1;
        RefreshGrid();
    }

    private void OnEnable()
    {
        var panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
        {
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnDisable()
    {
        var panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnUIReload(PanelRenderer pr, VisualElement root)
    {
        m_viewport = root.QOrFail<VisualElement>("InventoryGridViewport");
        m_capacity_label = root.QOrFail<Label>("InventoryCapacityLabel");
        RefreshGrid();
    }

    public void OpenInventory(IInventoryOwner owner)
    {
        this.gameObject.SetActive(true);
        SetInventoryOwner(owner);
    }
    
    public void CloseInventory(IInventoryOwner owner)
    {
        this.gameObject.SetActive(false);
        SetInventoryOwner(null);
    }

    private void Update()
    {
        if (m_viewport == null || m_inventory_owner == null) return;

        Data_Inventory dataInventory = m_inventory_owner.GetInventory();
        if (dataInventory == null) return;

        List<string> signature = BuildSignature(dataInventory);
        if (!SignatureMatches(signature, m_bound_signature))
        {
            RefreshGrid();
        }
    }

    // -------------------------------------------------------------
    // Pick-up / place
    // -------------------------------------------------------------

    private void HandleSlotClicked(int index)
    {
        OnSlotClicked?.Invoke(index);

        Data_Inventory dataInventory = m_inventory_owner?.GetInventory();
        if (dataInventory == null) return;

        if (m_held_slot_index < 0)
        {
            // Nothing held yet — picking up requires a non-empty slot.
            Data_InventorySlot slot = dataInventory.GetSlot(index);
            if (slot != null && !slot.IsEmpty)
            {
                m_held_slot_index = index;
            }
            ApplyHeldHighlight();
            return;
        }

        if (m_held_slot_index == index)
        {
            // Clicked the held slot again — cancel the pick-up.
            m_held_slot_index = -1;
            ApplyHeldHighlight();
            return;
        }

        if (!dataInventory.TryMoveSlot(m_held_slot_index, index, out string error))
        {
            TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN, $"Could not move slot {m_held_slot_index} -> {index}: {error}");
            // Keep holding — let the player pick a different destination.
            return;
        }

        m_held_slot_index = -1;
        RefreshGrid();
    }

    // -------------------------------------------------------------
    // Rendering
    // -------------------------------------------------------------

    private void RefreshGrid()
    {
        if (m_viewport == null || m_inventory_owner == null) return;

        Data_Inventory dataInventory = m_inventory_owner.GetInventory();
        if (dataInventory == null) return;

        m_viewport.Clear();
        m_slot_elements.Clear();
        m_bound_signature.Clear();
        m_bound_signature.AddRange(BuildSignature(dataInventory));

        for (int i = 0; i < dataInventory.MaxSlots; i++)
        {
            VisualElement slotElement = BuildSlotElement(dataInventory.GetSlot(i));
            int capturedIndex = i; // avoid closure-over-loop-variable bug
            slotElement.RegisterCallback<ClickEvent>(_ => HandleSlotClicked(capturedIndex));

            m_viewport.Add(slotElement);
            m_slot_elements.Add(slotElement);
        }

        ApplyHeldHighlight();

        if (m_capacity_label != null)
        {
            m_capacity_label.text = $"{dataInventory.UsedSlots} / {dataInventory.MaxSlots}";
        }
    }

    private static VisualElement BuildSlotElement(Data_InventorySlot slot)
    {
        var element = new VisualElement();
        element.AddToClassList("inventory-slot");

        if (slot == null || slot.IsEmpty)
        {
            element.AddToClassList("inventory-slot-empty");
            return element;
        }

        element.AddToClassList("inventory-slot-filled");

        if (slot.slot_type == InventorySlotType.Item)
        {
            //var tierBadge = new Label($"T{(int)slot.tier + 1}");
            //tierBadge.AddToClassList("inventory-slot-tier-badge");
            //element.Add(tierBadge);

            var label = new Label(FriendlyName(slot.item_type.ToString()));
            label.AddToClassList("inventory-slot-label");
            element.Add(label);

            var countBadge = new Label(slot.count > 1 ? $"x{slot.count}" : "");
            countBadge.AddToClassList("inventory-slot-count-badge");
            element.Add(countBadge);
        }
        else if (slot.slot_type == InventorySlotType.Module)
        {
            element.AddToClassList("inventory-slot-module");

            string partName = slot.module != null && !string.IsNullOrEmpty(slot.module.part_name)
                ? FriendlyName(slot.module.part_name)
                : "UNNAMED";

            var label = new Label(partName);
            label.AddToClassList("inventory-slot-label");
            element.Add(label);

            //if (slot.module != null)
            //{
            //    var stateBadge = new Label(slot.module.install_state.ToString());
            //    stateBadge.AddToClassList("inventory-slot-module-badge");
            //    element.Add(stateBadge);
            //}
        }

        return element;
    }

    private void ApplyHeldHighlight()
    {
        for (int i = 0; i < m_slot_elements.Count; i++)
        {
            if (i == m_held_slot_index)
            {
                m_slot_elements[i].AddToClassList("inventory-slot-held");
            }
            else
            {
                m_slot_elements[i].RemoveFromClassList("inventory-slot-held");
            }
        }
    }

    private static List<string> BuildSignature(Data_Inventory dataInventory)
    {
        var signature = new List<string>(dataInventory.MaxSlots);
        for (int i = 0; i < dataInventory.MaxSlots; i++)
        {
            Data_InventorySlot slot = dataInventory.GetSlot(i);
            if (slot == null || slot.IsEmpty)
            {
                signature.Add("EMPTY");
            }
            else if (slot.slot_type == InventorySlotType.Item)
            {
                signature.Add($"ITEM:{slot.item_type}:{slot.tier}:{slot.count}");
            }
            else
            {
                signature.Add($"MODULE:{slot.module?.part_name}:{slot.module?.install_state}");
            }
        }
        return signature;
    }

    private static bool SignatureMatches(List<string> a, List<string> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    // "SheetMetal" -> "SHEET METAL". Enum names have no separators, unlike
    // EquipmentType's SCREAMING_SNAKE_CASE, so ToUpperInvariant alone isn't readable.
    private static string FriendlyName(string raw)
    {
        var sb = new StringBuilder(raw.Length + 4);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(raw[i - 1]))
            {
                sb.Append(' ');
            }
            sb.Append(c);
        }
        return sb.ToString().ToUpperInvariant();
    }
}