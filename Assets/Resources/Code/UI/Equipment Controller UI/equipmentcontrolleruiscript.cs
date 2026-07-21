using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives two mirrored vertical carousels:
///   - Left: available tools (Controller_Equipment.GetAvailableTools())
///   - Right: currently compatible ship parts (Controller_Equipment.GetDisplayingParts())
/// Both keep their selection vertically centered, show up to 5 boxes at once,
/// fade toward the periphery, and wrap around in both directions.
///
/// Scroll input is a single up/down pair � Controller_Equipment.ScrollUp/ScrollDown
/// already decide internally whether that means "scroll parts" (when a slot's
/// compatible parts are being displayed) or "scroll tools" (otherwise), so this
/// script doesn't need to make that choice itself; it just reflects whichever
/// index changed.
/// </summary>
public class Component_ToolInventoryUI : MonoBehaviour
{
    private const float BOX_HEIGHT = 84f;
    private const float BOX_SPACING = 96f; // box height + margin, must match .tool-box in USS
    private const int MAX_VISIBLE_DISTANCE = 2; // 2 above + center + 2 below = 5 boxes

    [SerializeField] private Controller_Equipment m_equipment_controller;

    // One of these per carousel column so positioning/refresh logic isn't duplicated.
    private class Carousel
    {
        public VisualElement Viewport;
        public readonly List<VisualElement> Boxes = new List<VisualElement>();
        public readonly List<string> BoundLabels = new List<string>();
        public string EmptyMessage;
    }

    private readonly Carousel m_tool_carousel = new Carousel { EmptyMessage = "// NO TOOLS AVAILABLE" };
    private readonly Carousel m_part_carousel = new Carousel { EmptyMessage = "// NO COMPATIBLE PARTS" };
    private VisualElement m_hover_preview;
    private Label m_hover_preview_label;

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
        m_tool_carousel.Viewport = root.QOrFail<VisualElement>("ToolInventoryViewport");
        m_part_carousel.Viewport = root.QOrFail<VisualElement>("PartInventoryViewport");
        m_hover_preview = root.QOrFail<VisualElement>("CurrentHover");
        m_hover_preview_label = root.QOrFail<Label>("CurrentHoverLabel");
        RefreshCarousel(m_tool_carousel, GetToolLabels());
        RefreshCarousel(m_part_carousel, GetPartLabels());
        RefreshHoverPreview();
    }

    private void RefreshHoverPreview()
    {
        try
        {
            IInteractable foo = m_equipment_controller.GetCurrentHover();
            if (foo == null)
            {
                m_hover_preview.style.display = DisplayStyle.None;
            }
            else
            {
                MonoBehaviour mb = foo as MonoBehaviour;
                if (mb != null)
                {
                    m_hover_preview.style.display = DisplayStyle.Flex;
                    m_hover_preview_label.text = mb.gameObject.name;
                }
                else
                {
                    m_hover_preview.style.display = DisplayStyle.None;
                }
            }
        }
        catch(Exception e)
        {
            Debug.Log($"{e.Message} {e.StackTrace}");
        }
        
    }

    private void Update()
    {
        RefreshHoverPreview();
        if (m_tool_carousel.Viewport == null || m_part_carousel.Viewport == null) return;

        var toolLabels = GetToolLabels();
        if (!ListsMatch(toolLabels, m_tool_carousel.BoundLabels))
            RefreshCarousel(m_tool_carousel, toolLabels);
        else
            UpdateCarouselPositions(m_tool_carousel, m_equipment_controller.GetSelectedToolIndex());

        var partLabels = GetPartLabels();
        if (!ListsMatch(partLabels, m_part_carousel.BoundLabels))
            RefreshCarousel(m_part_carousel, partLabels);
        else
            UpdateCarouselPositions(m_part_carousel, m_equipment_controller.GetSelectedPartIndex());
    }


    public void ScrollUp() => m_equipment_controller.ScrollUp();
    public void ScrollDown() => m_equipment_controller.ScrollDown();

    private List<string> GetToolLabels()
    {
        var tools = m_equipment_controller.GetAvailableTools();
        var labels = new List<string>(tools.Count);
        foreach (var tool in tools)
            labels.Add(tool.ToString().ToUpperInvariant());
        return labels;
    }

    private List<string> GetPartLabels()
    {
        var parts = m_equipment_controller.GetDisplayingParts();
        var labels = new List<string>(parts?.Count ?? 0);
        if (parts == null) return labels;

        foreach (var data in parts)
            labels.Add(string.IsNullOrEmpty(data.part_name) ? "UNNAMED PART" : data.part_name.ToUpperInvariant());
        return labels;
    }

    private void RefreshCarousel(Carousel carousel, List<string> labels)
    {
        if (carousel.Viewport == null) return;

        carousel.Viewport.Clear();
        carousel.Boxes.Clear();
        carousel.BoundLabels.Clear();
        carousel.BoundLabels.AddRange(labels);

        if (carousel.BoundLabels.Count == 0)
        {
            var emptyLabel = new Label(carousel.EmptyMessage);
            emptyLabel.AddToClassList("tool-inventory-empty-label");
            carousel.Viewport.Add(emptyLabel);
            return;
        }

        foreach (var text in carousel.BoundLabels)
        {
            var box = new VisualElement();
            box.AddToClassList("tool-box");

            var label = new Label(text);
            label.AddToClassList("tool-box-label");
            box.Add(label);

            // Anchor every box at vertical center; per-frame translate handles offset.
            box.style.top = new Length(50, LengthUnit.Percent);
            box.style.marginTop = -BOX_HEIGHT / 2f;

            carousel.Viewport.Add(box);
            carousel.Boxes.Add(box);
        }
    }

    private void UpdateCarouselPositions(Carousel carousel, int selected)
    {
        int count = carousel.Boxes.Count;
        if (count == 0) return;

        if (selected < 0 || selected >= count) selected = 0;

        for (int i = 0; i < count; i++)
        {
            var box = carousel.Boxes[i];
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

    private static bool ListsMatch(List<string> a, List<string> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}