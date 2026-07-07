using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Vertical scrolling tool carousel. Keeps the currently selected tool
/// vertically centered, shows up to 5 boxes at once, and fades boxes
/// toward the periphery. Wraps around in both directions.
/// Scrolling drives Controller_Equipment's tool selection directly,
/// so the centered box is always the equipped tool.
/// </summary>
public class Component_ToolInventoryUI : MonoBehaviour
{
    private const float BOX_HEIGHT = 64f;
    private const float BOX_SPACING = 76f; // box height + margin, must match .tool-box in USS
    private const int MAX_VISIBLE_DISTANCE = 2; // 2 above + center + 2 below = 5 boxes

    [SerializeField] private Controller_Equipment m_equipment_controller;

    private VisualElement rootContainer;
    private VisualElement ToolInventoryViewport;

    private readonly List<VisualElement> m_boxes = new List<VisualElement>();
    private readonly List<EquipmentType> m_bound_tools = new List<EquipmentType>();
    private Label m_empty_label;

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
        rootContainer = root;

        ToolInventoryViewport = rootContainer.QOrFail<VisualElement>("ToolInventoryViewport");

        Refresh();
    }

    private void Update()
    {
       

        // Available tools can change at runtime (pickups, unlocks) — cheap
        // enough to compare against last frame and only rebuild on change.
        var current = m_equipment_controller.GetAvailableTools();
        if (!ListsMatch(current, m_bound_tools))
        {
            Refresh();
        }
        else
        {
            UpdateBoxPositions();
        }
    }

  

    private void Refresh()
    {
        

        ToolInventoryViewport.Clear();
        m_boxes.Clear();
        m_bound_tools.Clear();
        m_bound_tools.AddRange(m_equipment_controller.GetAvailableTools());

        if (m_bound_tools.Count == 0)
        {
            m_empty_label = new Label("// NO TOOLS AVAILABLE");
            m_empty_label.AddToClassList("tool-inventory-empty-label");
            ToolInventoryViewport.Add(m_empty_label);
            return;
        }

        foreach (var tool in m_bound_tools)
        {
            var box = new VisualElement();
            box.AddToClassList("tool-box");

            var label = new Label(tool.ToString().ToUpperInvariant());
            label.AddToClassList("tool-box-label");
            box.Add(label);

            // Anchor every box at vertical center; per-frame translate handles offset.
            box.style.top = new Length(50, LengthUnit.Percent);
            box.style.marginTop = -BOX_HEIGHT / 2f;

            ToolInventoryViewport.Add(box);
            m_boxes.Add(box);
        }

        UpdateBoxPositions();
    }

    private void UpdateBoxPositions()
    {
        if (m_boxes.Count == 0) return;

        int count = m_boxes.Count;
        int selected = m_equipment_controller.GetSelectedToolIndex();
        if (selected < 0 || selected >= count) selected = 0;

        for (int i = 0; i < count; i++)
        {
            var box = m_boxes[i];
            int diff = CircularDiff(i, selected, count);
            int absDiff = Mathf.Abs(diff);

            box.RemoveFromClassList("tool-box-selected");
            box.RemoveFromClassList("tool-box-dist-1");
            box.RemoveFromClassList("tool-box-dist-2");

            if (absDiff > MAX_VISIBLE_DISTANCE)
            {
                box.style.display = DisplayStyle.None;
                continue;
            }

            box.style.display = DisplayStyle.Flex;
            box.style.translate = new Translate(0, diff * BOX_SPACING);

            if (diff == 0) box.AddToClassList("tool-box-selected");
            else if (absDiff == 1) box.AddToClassList("tool-box-dist-1");
            else if (absDiff == 2) box.AddToClassList("tool-box-dist-2");
        }
    }

    // Shortest signed distance from `selected` to `index` around a circular list of size `count`.
    private static int CircularDiff(int index, int selected, int count)
    {
        int raw = ((index - selected) % count + count) % count; // normalize to [0, count)
        if (raw > count / 2) raw -= count; // fold into (-count/2, count/2]
        return raw;
    }

    private static bool ListsMatch(List<EquipmentType> a, List<EquipmentType> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}