using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipmentType
{
    NONE,
    SOCKET_WRENCH,
    BUTANE_TORCH,
    PRY_BAR,
    SCREW_DRIVER,
    PLASMA_TORCH,
    SOCKET_DRILL,
    SHIP_BUILDER
}

[RequireComponent(typeof(Mediator_PlayerMiniGames))]

public class Controller_Equipment : MonoBehaviour, IInventoryOwner
{
    [Header("Tools")]
    [SerializeField] List<EquipmentType> m_available_tools = new List<EquipmentType>();
    [SerializeField] private EquipmentType m_equipped_tool;
    private int m_selected_tool_index;

    [Header("Ship Parts")]
    [SerializeField] List<GameObject> m_starting_part_prefabs;

    [SerializeField] private Mediator_PlayerMiniGames m_minigame_mediator;

    private Inventory m_inventory = new Inventory();

    private int m_selected_part_index;
    private IInteractable current_hover_interactable;
    private bool m_build_mode;
    
    private GameObject currentGhost;
    private Data_ShipModule _currentGhostModuleData;

    private void Awake()
    {
        m_minigame_mediator = GetComponent<Mediator_PlayerMiniGames>();

        // Seed starting inventory from prefab defaults � clone so multiple
        // starting parts sharing one prefab don't share mutable state.
        foreach (var prefabGO in m_starting_part_prefabs)
        {
            var sourcePart = prefabGO.GetComponent<Component_ShipPart>();
            if (sourcePart == null)
            {
                TopicLogger.Log(LogTopic.Equipment_Controller, LogLevel.ERROR,
                    $"{prefabGO.name} is missing Component_ShipPart");
                continue;
            }

            Data_ShipModule cloned = sourcePart.GetData().Clone();
            if (!m_inventory.TryAddModule(cloned, out string error))
            {
                TopicLogger.Log(LogTopic.Equipment_Controller, LogLevel.ERROR,
                    $"Failed to seed starting part {prefabGO.name}: {error}");
            }
        }
        m_starting_part_prefabs.Clear();
    }

    public void ActivateTool()
    {
        if(this.m_equipped_tool == EquipmentType.SHIP_BUILDER)
        {
            this.m_build_mode = !this.m_build_mode;

            if (!this.m_build_mode)
            {
                (current_hover_interactable as IHighlightable)?.SetHighlight(InteractionHighlightState.NONE, this);
            }
        }
    }
    public void ScrollDown()
    {
        if (this.m_build_mode)
        {
            this.ScrollEquippedPartDown();
        }
        else
        {
            this.ScrollToolSelectionDown();
        }
    }

    public void ScrollUp()
    {
        if (this.m_build_mode)
        {
            this.ScrollEquippedPartUp();
        }
        else
        {
            this.ScrollToolSelectionUp();
        }
    }

    public void EquipTool(EquipmentType type)
    {
        if (m_available_tools.Contains(type))
        {
            m_equipped_tool = type;
        }
        else
        {
            //we dont have this tool 
        }
        if (m_equipped_tool != EquipmentType.SHIP_BUILDER)
        {
            (current_hover_interactable as IHighlightable)?.SetHighlight(InteractionHighlightState.NONE, this);
        }
    }

    // Called when a live part (e.g. detached from the ship) needs to move into the backpack.
    // Syncs its live state into its Data_ShipPart, stores the data, then destroys the view.
    public bool TryAddPartToInventory(Component_ShipPart part)
    {
        if (part == null)
        {
            TopicLogger.Log(LogTopic.Equipment_Controller, LogLevel.ERROR, "Attempted to add null part");
            return false;
        }

        Data_ShipModule data = part.GetData();

        if (m_inventory.ContainsModule(data))
        {
            return false; // already added
        }

        if (!m_inventory.TryAddModule(data, out string error))
        {
            // Inventory full (or other failure) — leave the physical part in
            // the world rather than destroying something we couldn't store.
            TopicLogger.Log(LogTopic.Equipment_Controller, LogLevel.WARN,
                $"Could not add part to inventory: {error}");
            return false;
        }

        return true;
    }

    public void RemovePartFromInventory(Data_ShipModule data)
    {
        if (data == null) return;

        if (!m_inventory.TryRemoveModule(data))
        {
            TopicLogger.Log(LogTopic.Equipment_Controller, LogLevel.ERROR,
                "Attempted to remove part not in inventory");
            return;
        }

        m_selected_part_index = Mathf.Clamp(m_selected_part_index, 0, Mathf.Max(0, m_inventory.GetModulesCompact().Count - 1));
    }

    public void ClearInventory()
    {
        m_inventory.ClearModules();
    }

    public EquipmentType GetEquippedTool()
    {
        return m_equipped_tool;
    }

    public List<EquipmentType> GetAvailableTools()
    {
        return m_available_tools;
    }

    public int GetSelectedToolIndex()
    {
        return m_selected_tool_index;
    }

    // Wrap-around scroll through m_available_tools. The centered tool becomes equipped.
    public void ScrollToolSelectionUp()
    {
        if (m_available_tools.Count == 0) return;

        m_selected_tool_index = (m_selected_tool_index + 1) % m_available_tools.Count;
        EquipTool(m_available_tools[m_selected_tool_index]);
    }

    public void ScrollToolSelectionDown()
    {
        if (m_available_tools.Count == 0) return;

        m_selected_tool_index = (m_selected_tool_index - 1 + m_available_tools.Count) % m_available_tools.Count;
        EquipTool(m_available_tools[m_selected_tool_index]);
    }


    public Inventory GetInventory()
    {
        return m_inventory;
    }

    public IReadOnlyList<Data_ShipModule> GetShipPartInventory()
    {
        return m_inventory.GetModulesCompact();
    }



    // The subset of inventory parts currently compatible with whatever slot
    // is being hovered (set via SetRelevantShipParts). This is what the
    // right-hand part carousel should render � not the full inventory.
    public IReadOnlyList<Data_ShipModule> GetDisplayingParts()
    {
        return m_inventory.GetModulesCompact();
    }

    public int GetSelectedPartIndex()
    {
        return m_selected_part_index;
    }

    public Data_ShipModule GetEquippedPart()
    {
        IReadOnlyList<Data_ShipModule> parts = m_inventory.GetModulesCompact();
        bool exists = m_selected_part_index >= 0 && m_selected_part_index < parts.Count;
        if (exists)
        {
            return parts[m_selected_part_index];
        }
        return null;
    }

    public void ScrollEquippedPartUp()
    {
        int count = m_inventory.GetModulesCompact().Count;
        if (count == 0) return;

        m_selected_part_index = (m_selected_part_index + 1) % count;
    }

    public void ScrollEquippedPartDown()
    {
        int count = m_inventory.GetModulesCompact().Count;
        if (count == 0) return;

        m_selected_part_index = (m_selected_part_index - 1 + count) % count;
    }


    public void Unequip()
    {
        m_equipped_tool = EquipmentType.NONE;
        if (m_equipped_tool != EquipmentType.SHIP_BUILDER)
        {
            (current_hover_interactable as IHighlightable)?.SetHighlight(InteractionHighlightState.NONE, this);
        }
    }

    public void startMiniGame(IToolMinigame game)
    {
        m_minigame_mediator.StartMiniGame(game);
    }

    public Controller_PlayerInput GetUIHub()
    {
        return m_minigame_mediator.GetUIHub();
    }

    internal void setCurrentHoverInteractable(IInteractable hit, RaycastHit hitInfo)
    {
        this.current_hover_interactable = hit;
    }

    public bool BuildMode()
    {
        return m_build_mode;
    }

    internal IInteractable GetCurrentHover()
    {
        return this.current_hover_interactable;
    }

   
}