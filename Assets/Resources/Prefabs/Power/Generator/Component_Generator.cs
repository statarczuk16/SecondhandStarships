using System;
using NUnit.Framework.Constraints;
using UnityEngine;

public class Component_Generator : MonoBehaviour, IToggleable, IInventoryOwner, IPowerGenerator, IPowerNetworked, IPowerCapacity
{
    [SerializeField] private Component_Inventory m_inventory;
    [SerializeField, Required] private Component_PowerNode m_connected_power_node;
    private bool m_is_on = false;
    [SerializeField] private bool m_wants_to_be_on;
    private AudioHandle m_generator_sound_handle = AudioHandle.Invalid;
    private const int POWER_PER_SECOND = 1000;
    private bool needs_startup_register = true;

    public void Awake()
    {
        this.m_inventory.ClaimInventory(this);
        
    }
    private void Update()
    {
        if (needs_startup_register)
        {
            needs_startup_register = TryGameStartNetworkConnect();
        }

        RefreshActualOnState();
    }
    
    private void RefreshActualOnState()
    {
        bool should_be_on = m_wants_to_be_on && OnRequirementsMet(out _);
        if (should_be_on == m_is_on)
        {
            return;
        }
        if (should_be_on) TurnOn();
        else TurnOff();
    }

    public bool CanToggle(out string reason)
    {
        reason = "NO_ISSUE";
        if (!m_inventory)
        {
            reason = "NO_INVENTORY_ATTACHED";
            return false;
        }

        if (m_inventory.GetInventory().HasAllRecipeItems())
        {
            return true;
        }
       
        reason = $"PARTS_MISSING // CHECK SERVICE HATCH";
        return false;
        
    }
    
    public void ToggleWantsToBeOn()
    {
        m_wants_to_be_on = !m_wants_to_be_on;
        RefreshActualOnState();
    }

    public bool IsOn()
    {
        return m_is_on;
    }

    public bool WantsToBeOn()
    {
        return m_wants_to_be_on;
    }

    public void TurnOff()
    {
        if (this.m_generator_sound_handle.IsValid)
        {
            AudioEvents.StopLoop(this.m_generator_sound_handle);
            this.m_generator_sound_handle = AudioHandle.Invalid;
        }

        m_is_on = false;
    }

    public void TurnOn()
    {
        if (!this.m_generator_sound_handle.IsValid)
        {
            this.m_generator_sound_handle = AudioEvents.StartLoop(SoundID.Generator, this.transform);
        }

        m_is_on = true;
    }

    public bool OnRequirementsMet(out string reason)
    {
        return CanToggle(out reason);
    }

    public Data_Inventory GetInventory()
    {
        return m_inventory.GetInventory();
    }

    public bool IsInstallTarget()
    {
        return true;
    }
    
    public Component_PowerNode TryFindPowerNode()
    {
        return m_connected_power_node;
        //generators must have their own built in nodes
    }

    public bool TryGameStartNetworkConnect()
    {
        if (m_connected_power_node == null)
        {
            m_connected_power_node = TryFindPowerNode();
        }

        if (m_connected_power_node == null)
        {
            return true;
        }
        if (m_connected_power_node.GetOwningNetwork() != null)
        {
            if (m_connected_power_node.GetOwningNetwork() != null)
            {
                ConnectToNode(m_connected_power_node);
                TopicLogger.Log(LogTopic.PowerSystem, LogLevel.INFO, $"First time connect! {this.name}");
                return false;
            }
        }
        return true;
    }

    public void ConnectToNode(Component_PowerNode node)
    {
        m_connected_power_node = node;
        node.ConnectGenerator(this);
        node.ConnectCapacitor(this);
    }

    public void DisconnectFromNode()
    {
        m_connected_power_node.DisconnectGenerator(this);
        m_connected_power_node.DisconnectCapacitor(this);
        m_connected_power_node = null;
        
    }

    public float GetPowerRadius_M()
    {
        return 0f;
    }

    public float GetPowerCapacity()
    {
        return 10;
    }

    public float GetPowerGeneratedPerDT(float dt)
    {
        if(IsOn())
        {
            return POWER_PER_SECOND * dt;
        }
        return 0f;
    }
}
