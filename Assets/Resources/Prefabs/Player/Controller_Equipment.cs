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
    SOCKET_DRILL
}

[RequireComponent(typeof(Mediator_PlayerMiniGames))]

public class Controller_Equipment : MonoBehaviour
{
    [Header("Tools")]
    [SerializeField] List<EquipmentType> m_available_tools = new List<EquipmentType>();
    [SerializeField] private EquipmentType m_equipped_tool;
    private int m_selected_tool_index;

    [Header("Ship Parts")]
    [SerializeField] List<GameObject> m_starting_part_prefabs;

    [SerializeField] private Mediator_PlayerMiniGames m_minigame_mediator;

    private List<Data_ShipPart> m_parts_in_inventory = new List<Data_ShipPart>();
    private List<Data_ShipPart> m_list_of_displaying_parts = new List<Data_ShipPart>();

    private int m_selected_part_index;
    private IInteractable current_hover;

    private void Awake()
    {
        m_minigame_mediator = GetComponent<Mediator_PlayerMiniGames>();
        m_parts_in_inventory = new List<Data_ShipPart>();

        // Seed starting inventory from prefab defaults — clone so multiple
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
            m_parts_in_inventory.Add(sourcePart.GetData().Clone());
        }
        m_starting_part_prefabs.Clear();
    }

    public void ScrollDown()
    {
        if (this.m_list_of_displaying_parts.Count > 0)
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
        if (this.m_list_of_displaying_parts.Count > 0)
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
    }

    // Called when a live part (e.g. detached from the ship) needs to move into the backpack.
    // Syncs its live state into its Data_ShipPart, stores the data, then destroys the view.
    public void AddPartToInventory(Component_ShipPart part)
    {
        if (part == null)
        {
            TopicLogger.Log(LogTopic.Equipment_Controller, LogLevel.ERROR, "Attempted to add null part");
            return;
        }


        Data_ShipPart data = part.GetData();

        if (m_parts_in_inventory.Contains(data))
        {
            return; // already added
        }

        m_parts_in_inventory.Add(data);
        Destroy(part.gameObject);
    }

    public void RemovePartFromInventory(Data_ShipPart data)
    {
        if (data == null) return;

        if (!m_parts_in_inventory.Contains(data))
        {
            TopicLogger.Log(LogTopic.Equipment_Controller, LogLevel.ERROR,
                "Attempted to remove part not in inventory");
            return;
        }

        m_parts_in_inventory.Remove(data);

        if (m_list_of_displaying_parts != null)
        {
            m_list_of_displaying_parts.Remove(data);
            m_selected_part_index = Mathf.Clamp(m_selected_part_index, 0, Mathf.Max(0, m_list_of_displaying_parts.Count - 1));
        }
    }

    public void ClearInventory()
    {
        m_parts_in_inventory.Clear();
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


    public List<Data_ShipPart> GetShipPartInventory()
    {
        return m_parts_in_inventory;
    }

    public void SetRelevantShipParts(List<Data_ShipPart> parts)
    {
        m_list_of_displaying_parts = parts;
        /**
        HashSet<Data_ShipPart> new_parts = new HashSet<Data_ShipPart>(parts);
        HashSet<Data_ShipPart> old_parts = new HashSet<Data_ShipPart>(m_list_of_displaying_parts);
        if(new_parts.SetEquals(old_parts) == false)
        {
            //dont refresh what part we're looking at in the UI unless the part list is actually different
            m_list_of_displaying_parts = parts;
            m_selected_part_index = 0;
        }
        **/
        
        
    }

    // The subset of inventory parts currently compatible with whatever slot
    // is being hovered (set via SetRelevantShipParts). This is what the
    // right-hand part carousel should render — not the full inventory.
    public List<Data_ShipPart> GetDisplayingParts()
    {
        return m_list_of_displaying_parts;
    }

    public int GetSelectedPartIndex()
    {
        return m_selected_part_index;
    }

    public Data_ShipPart GetEquippedPart()
    {
        bool exists = m_selected_part_index >= 0 && m_selected_part_index < m_list_of_displaying_parts.Count;
        if (exists)
        {
            return m_list_of_displaying_parts[m_selected_part_index];
        }
        return null;
    }

    public void ScrollEquippedPartUp()
    {
        if (m_list_of_displaying_parts.Count == 0) return;

        m_selected_part_index = (m_selected_part_index + 1) % m_list_of_displaying_parts.Count;
    }

    public void ScrollEquippedPartDown()
    {
        if (m_list_of_displaying_parts.Count == 0) return;

        m_selected_part_index = (m_selected_part_index - 1 + m_list_of_displaying_parts.Count) % m_list_of_displaying_parts.Count;
    }


    public void Unequip()
    {
        m_equipped_tool = EquipmentType.NONE;
    }

    public void startMiniGame(IToolMinigame game)
    {
        m_minigame_mediator.StartMiniGame(game);
    }

    public Controller_PlayerInput GetUIHub()
    {
        return m_minigame_mediator.GetUIHub();
    }

    internal void setCurrentHover(IInteractable hit)
    {
        this.current_hover = hit;
    }

    internal IInteractable GetCurrentHover()
    {
        return this.current_hover;
    }
}