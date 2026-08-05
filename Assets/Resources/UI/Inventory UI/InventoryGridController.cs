using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Renders a single inventory grid. Managed by DualInventoryController.
/// </summary>
public class InventoryGridController
{
    public IInventoryOwner InventoryOwner { get; private set; }
    public bool HasOwner => InventoryOwner != null;

    public event Action<int> OnSlotRightClicked;
    public event Action<int> OnSlotClicked;

    private readonly string m_debugName;
    private VisualElement m_rootElement;
    private VisualElement m_viewport;
    private Label m_capacity_label;
    private VisualElement m_recipeFooter;
    private VisualElement m_recipeFooter2;

    private readonly List<VisualElement> m_slot_elements = new List<VisualElement>();
    private readonly List<string> m_bound_signature = new List<string>();

    private int m_held_slot_index = -1;

    public InventoryGridController(string debugName)
    {
        m_debugName = debugName;
    }

    private void HandleSlotRightClicked(int index)
    {
        OnSlotRightClicked?.Invoke(index);
    }
    
    public void Initialize(VisualElement rootElement)
    {
        m_rootElement = rootElement;
        if (m_rootElement == null) return;

        m_viewport = m_rootElement.Q<VisualElement>("InventoryGridViewport");
        m_capacity_label = m_rootElement.Q<Label>("InventoryCapacityLabel");
        m_recipeFooter = m_rootElement.Q<VisualElement>("RecipeFooter");
        m_recipeFooter2 = m_rootElement.Q<VisualElement>("RecipeFooter2");
        
        RefreshGrid();
    }

    public void SetInventoryOwner(IInventoryOwner owner)
    {
        InventoryOwner = owner;
        SetHeldIndex(-1);
        RefreshGrid();
    }

    public void Tick()
    {
        if (m_viewport == null || InventoryOwner == null) return;

        Data_Inventory dataInventory = InventoryOwner.GetInventory();
        if (dataInventory == null) return;

        List<string> signature = BuildSignature(dataInventory);
        if (!SignatureMatches(signature, m_bound_signature))
        {
            RefreshGrid();
        }
    }

    public void SetHeldIndex(int index)
    {
        m_held_slot_index = index;
        ApplyHeldHighlight();
    }

    private void HandleSlotClicked(int index)
    {
        OnSlotClicked?.Invoke(index);
    }

    // -------------------------------------------------------------
    // Rendering
    // -------------------------------------------------------------

    private void RefreshGrid()
    {
        if (m_viewport == null) return;

        if (InventoryOwner == null)
        {
            m_viewport.Clear();
            if (m_recipeFooter != null) m_recipeFooter.style.display = DisplayStyle.None;
            if (m_recipeFooter2 != null) m_recipeFooter2.style.display = DisplayStyle.None;
            return;
        }

        Data_Inventory dataInventory = InventoryOwner.GetInventory();
        if (dataInventory == null) return;

        m_viewport.Clear();
        m_slot_elements.Clear();
        m_bound_signature.Clear();
        m_bound_signature.AddRange(BuildSignature(dataInventory));

        for (int i = 0; i < dataInventory.MaxSlots; i++)
        {
            VisualElement slotElement = BuildSlotElement(dataInventory.GetSlot(i));
            int capturedIndex = i;
            slotElement.RegisterCallback<PointerUpEvent>(evt => 
            {
                // button 0 is Left Click
                if (evt.button == 0)
                {
                    HandleSlotClicked(capturedIndex);
                }
                // button 1 is Right Click
                else if (evt.button == 1)
                {
                    HandleSlotRightClicked(capturedIndex);
                }
            });

            m_viewport.Add(slotElement);
            m_slot_elements.Add(slotElement);
        }

        ApplyHeldHighlight();

        if (m_capacity_label != null)
        {
            m_capacity_label.text = $"{dataInventory.UsedSlots} / {dataInventory.MaxSlots}";
        }

        RefreshRecipeFooter(dataInventory);
    }

    private void RefreshRecipeFooter(Data_Inventory dataInventory)
    {
        if (m_recipeFooter == null) return;

        if (dataInventory.Recipe == null || dataInventory.Recipe.Count == 0)
        {
            m_recipeFooter.style.display = DisplayStyle.None;
            m_recipeFooter2.style.display = DisplayStyle.None;
            return;
        }

        m_recipeFooter.style.display = DisplayStyle.Flex;
        m_recipeFooter.Clear();
        m_recipeFooter2.style.display = DisplayStyle.Flex;
        m_recipeFooter2.Clear();

        // 1. Tally requirements exactly as Data_Inventory does internally
        var required = new Dictionary<(ItemType, Tier), int>();
        foreach (var ingredient in dataInventory.Recipe)
        {
            var key = (ingredient.item_type, ingredient.tier);
            required.TryGetValue(key, out int count);
            required[key] = count + 1;
        }

        // 2. Build Row 1: The individual items
        VisualElement itemsRow = new VisualElement();
        itemsRow.style.flexDirection = FlexDirection.Row;
        itemsRow.style.flexWrap = Wrap.Wrap;
        itemsRow.style.justifyContent = Justify.Center;
        itemsRow.style.marginBottom = 6;

        bool allMet = true;

        foreach (var kvp in required)
        {
            int need = kvp.Value;
            int have = dataInventory.GetItemCount(kvp.Key.Item1, kvp.Key.Item2);
            bool isMet = have >= need;

            if (!isMet) allMet = false;

            string itemName = FriendlyName(kvp.Key.Item1.ToString());
            string symbol = isMet ? "✓" : "✗";
            
            Label reqLabel = new Label($"{need} {itemName} {symbol}");
            reqLabel.style.marginRight = 8;
            reqLabel.style.color = isMet ? new StyleColor(new Color(0.3f, 0.8f, 0.3f)) : new StyleColor(new Color(0.9f, 0.3f, 0.3f));
            
            itemsRow.Add(reqLabel);
        }

        // 3. Build Row 2: Overall status
        VisualElement overallRow = new VisualElement();
        overallRow.style.flexDirection = FlexDirection.Row;
        overallRow.style.justifyContent = Justify.Center;

        Label overallLabel = new Label(allMet ? "//READY" : "//MISSING PARTS");
        overallLabel.style.color = allMet ? new StyleColor(new Color(0.3f, 0.8f, 0.3f)) : new StyleColor(new Color(0.9f, 0.3f, 0.3f));
        overallLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        overallRow.Add(overallLabel);

        m_recipeFooter.Add(itemsRow);
        m_recipeFooter2.Add(overallRow);
    }

    private static VisualElement BuildSlotElement(Data_InventorySlot slot)
    {
        var element = new VisualElement();
        element.AddToClassList("inventory-slot");

        if (slot == null || slot.IsEmpty)
        {
            element.AddToClassList("inventory-slot-empty");
            element.AddToClassList("theme-bg-panel-alt");
            element.AddToClassList("theme-border-subtle");
            return element;
        }

        element.AddToClassList("inventory-slot-filled");
        element.AddToClassList("theme-bg-panel");
        element.AddToClassList("theme-border");

        if (slot.slot_type == InventorySlotType.Item)
        {
            var label = new Label(FriendlyName(slot.item_type.ToString()));
            label.AddToClassList("inventory-slot-label");
            label.AddToClassList("theme-text-primary");
            element.Add(label);

            var countBadge = new Label(slot.count > 1 ? $"x{slot.count}" : "");
            countBadge.AddToClassList("inventory-slot-count-badge");
            countBadge.AddToClassList("theme-text-primary");
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
            label.AddToClassList("theme-text-accent");
            element.Add(label);
        }

        return element;
    }

    private void ApplyHeldHighlight()
    {
        for (int i = 0; i < m_slot_elements.Count; i++)
        {
            var element = m_slot_elements[i];

            if (i == m_held_slot_index)
            {
                element.AddToClassList("inventory-slot-held");
                element.RemoveFromClassList("theme-bg-panel");
                element.RemoveFromClassList("theme-border");
                
                element.AddToClassList("theme-bg-raised");
                element.AddToClassList("theme-border-active");
            }
            else
            {
                element.RemoveFromClassList("inventory-slot-held");
                element.RemoveFromClassList("theme-bg-raised");
                element.RemoveFromClassList("theme-border-active");

                if (!element.ClassListContains("inventory-slot-empty"))
                {
                    element.AddToClassList("theme-bg-panel");
                    element.AddToClassList("theme-border");
                }
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
        
        // Add the recipe to the signature so the UI refreshes if the recipe changes
        if (dataInventory.Recipe != null)
        {
            foreach (var req in dataInventory.Recipe)
            {
                signature.Add($"RECIPE:{req.item_type}:{req.tier}");
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