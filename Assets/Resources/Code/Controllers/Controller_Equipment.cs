using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipmentType
{
    None,
    Wrench,
    PlasmaCutter,
    PryBar,
}

[RequireComponent(typeof(Mediator_PlayerMiniGames))]

public class Controller_Equipment : MonoBehaviour
{
    [Header("Tools")]
    [SerializeField] List<EquipmentType> m_available_tools;
    [SerializeField] private EquipmentType m_equipped_tool;

    [Header("Ship Parts")]
    [SerializeField] List<GameObject> m_starting_part_prefabs;

    [SerializeField] private Mediator_PlayerMiniGames m_minigame_mediator;

    private List<Data_ShipPart> m_parts_in_inventory;
    private List<Data_ShipPart> m_list_of_displaying_parts;

    private int m_selected_part_index;


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

    public List<Data_ShipPart> GetShipPartInventory()
    {
        return m_parts_in_inventory;
    }

    public void SetRelevantShipParts(List<Data_ShipPart> parts)
    {
        m_list_of_displaying_parts = parts;
        m_selected_part_index = 0;
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

    public void ScrollUpEquippedTool()
    {
        if (m_list_of_displaying_parts.Count == 0) return;

        m_selected_part_index = (m_selected_part_index + 1) % m_list_of_displaying_parts.Count;
    }

    public void ScrollDownEquippedTool()
    {
        if (m_list_of_displaying_parts.Count == 0) return;

        m_selected_part_index = (m_selected_part_index - 1 + m_list_of_displaying_parts.Count) % m_list_of_displaying_parts.Count;
    }

    public void Unequip()
    {
        m_equipped_tool = EquipmentType.None;
    }

    public void startMiniGame(IToolMinigame game)
    {
        m_minigame_mediator.StartMiniGame(game);
    }

    public Controller_PlayerInput GetUIHub()
    {
        return m_minigame_mediator.GetUIHub();
    }
}