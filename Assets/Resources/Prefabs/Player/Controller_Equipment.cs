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
    [SerializeField] private  DualInventoryController m_inventory_controller;
    
    [SerializeField] private Data_Inventory _mDataInventory = new Data_Inventory();

    private int m_selected_part_index;
    private IInteractable current_hover_interactable;
    private bool m_tool_active;
    
    private GameObject currentGhost;
    private Data_ShipModule _currentGhostModuleData;
    private Dictionary<EquipmentType, string> m_tooltips = new()
    {
        { EquipmentType.SOCKET_WRENCH, "LMB Bolt/Unbolt" },
        { EquipmentType.SHIP_BUILDER , "LMB Install Part\nRMB Toggle Build Mode"}
    };

    public string GetToolTipForEquipment(EquipmentType type)
    {
        if (m_tooltips.ContainsKey(type))
        {
            return  m_tooltips[type];
        }

        return "";
    }

    public float GetHeatPerSecond()
    {
        if (TorchMode())
        {
            if (m_equipped_tool == EquipmentType.BUTANE_TORCH)
            {
                return 50;
            }
        }

        return 0f;
    }

    public string GetToolTip()
    {
        string base_tooltip = GetToolTipForEquipment(this.GetEquippedTool());
        switch (this.GetEquippedTool())
        {
            case EquipmentType.SHIP_BUILDER:
            {
                if (this.m_tool_active)
                {
                    base_tooltip += "\n//BUILD_MODE";
                }
                else
                {
                    base_tooltip += "\n//WAITING...";
                }
                break;
            }
        }

        return base_tooltip;
    }

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
            if (!_mDataInventory.TryAddModule(cloned, out string error))
            {
                TopicLogger.Log(LogTopic.Equipment_Controller, LogLevel.ERROR,
                    $"Failed to seed starting part {prefabGO.name}: {error}");
            }
        }
        m_starting_part_prefabs.Clear();
    }

    public void ActivateTool()
    {
        this.m_tool_active = !this.m_tool_active;
        if(this.m_equipped_tool == EquipmentType.SHIP_BUILDER)
        {
            if (!this.m_tool_active)
            {
                (current_hover_interactable as IHighlightable)?.SetHighlight(InteractionHighlightState.NONE, this);
            }
        }
    }
    public void ScrollDown()
    {
        if (this.m_tool_active && this.m_equipped_tool == EquipmentType.SHIP_BUILDER)
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
        if (this.m_tool_active && this.m_equipped_tool == EquipmentType.SHIP_BUILDER)
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

        if (_mDataInventory.ContainsModule(data))
        {
            return false; // already added
        }

        if (!_mDataInventory.TryAddModule(data, out string error))
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

        if (!_mDataInventory.TryRemoveModule(data))
        {
            TopicLogger.Log(LogTopic.Equipment_Controller, LogLevel.ERROR,
                "Attempted to remove part not in inventory");
            return;
        }

        m_selected_part_index = Mathf.Clamp(m_selected_part_index, 0, Mathf.Max(0, _mDataInventory.GetModulesCompact().Count - 1));
    }

    public void ClearInventory()
    {
        _mDataInventory.ClearModules();
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


    public Data_Inventory GetInventory()
    {
        return _mDataInventory;
    }

    public bool IsInstallTarget()
    {
        return false;
    }

    public IReadOnlyList<Data_ShipModule> GetShipPartInventory()
    {
        return _mDataInventory.GetModulesCompact();
    }



    // The subset of inventory parts currently compatible with whatever slot
    // is being hovered (set via SetRelevantShipParts). This is what the
    // right-hand part carousel should render � not the full inventory.
    public IReadOnlyList<Data_ShipModule> GetDisplayingParts()
    {
        return _mDataInventory.GetModulesCompact();
    }

    public int GetSelectedPartIndex()
    {
        return m_selected_part_index;
    }

    public Data_ShipModule GetEquippedPart()
    {
        IReadOnlyList<Data_ShipModule> parts = _mDataInventory.GetModulesCompact();
        bool exists = m_selected_part_index >= 0 && m_selected_part_index < parts.Count;
        if (exists)
        {
            return parts[m_selected_part_index];
        }
        return null;
    }

    public void ScrollEquippedPartUp()
    {
        int count = _mDataInventory.GetModulesCompact().Count;
        if (count == 0) return;

        m_selected_part_index = (m_selected_part_index + 1) % count;
    }

    public void ScrollEquippedPartDown()
    {
        int count = _mDataInventory.GetModulesCompact().Count;
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

    internal void setCurrentInteractable(IInteractable hit, RaycastHit hitInfo)
    {
        this.current_hover_interactable = hit;
    }

    public bool BuildMode()
    {
        return (this.m_tool_active && this.m_equipped_tool == EquipmentType.SHIP_BUILDER);
    }
    
    public bool TorchMode()
    {
        return (this.m_tool_active && this.m_equipped_tool == EquipmentType.BUTANE_TORCH);
    }

    internal IInteractable GetCurrentHover()
    {
        return this.current_hover_interactable;
    }


    public void OpenPlayerMenu()
    {
        DisplayInventory(this, null);//opens player inventory
    }

    public void ClosePlayerMenu()
    {
        this.m_inventory_controller.CloseUI();
        m_minigame_mediator.ChangeInputMode(InputMode.MovingMode);
    }

    public void DisplayInventory(IInventoryOwner left_inventory, IInventoryOwner right_inventory)
    {
        this.m_inventory_controller.OpenUI(this, left_inventory, right_inventory);
        m_minigame_mediator.ChangeInputMode(InputMode.MenuMode);
    }

    public void OnHoverExit(IInteractable mCurrentInteractable)
    {
        mCurrentInteractable?.OnHoverExit(this);
    }

    public void OnHoverEnter(IInteractable mCurrentInteractable)
    {
        mCurrentInteractable?.OnHoverEnter(this);
    }

    public void OnHoverUpdate(IInteractable mCurrentInteractable, RaycastHit hitInfo)
    {
        mCurrentInteractable?.OnHoverUpdate(this, hitInfo);
    }
}