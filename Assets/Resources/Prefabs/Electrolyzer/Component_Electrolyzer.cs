using System;
using NUnit.Framework.Constraints;
using UnityEngine;

public class Component_Electrolyzer : MonoBehaviour, IToggleable, IInventoryOwner 
{
    [SerializeField] private Component_Inventory m_inventory;
    [SerializeField] private Component_Converter m_converter;
    private bool m_wants_to_be_on;
    private bool m_is_on;
    private AudioHandle m_electrolyzer_sound_handle = AudioHandle.Invalid;

    private void Update()
    {
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

    public bool OnRequirementsMet(out string reason)
    {
        return CanToggle(out reason);
    }

    public void ToggleWantsToBeOn()
    {
        m_wants_to_be_on = !m_wants_to_be_on;
        RefreshActualOnState();
    }

    public void TurnOn()
    {
        if (!this.m_electrolyzer_sound_handle.IsValid)
        {
            this.m_electrolyzer_sound_handle = AudioEvents.StartLoop(SoundID.Generator, this.transform);
        }
        m_converter.TurnOn();
        m_is_on = true;
    }

    public void TurnOff()
    {
        if (this.m_electrolyzer_sound_handle.IsValid)
        {
            AudioEvents.StopLoop(this.m_electrolyzer_sound_handle);
            this.m_electrolyzer_sound_handle = AudioHandle.Invalid;
        }
        m_converter.TurnOff();
        m_is_on = false;
    }

    public bool IsOn()
    {
        return m_is_on;
    }

    public bool WantsToBeOn()
    {
        return m_wants_to_be_on;
    }

    public Data_Inventory GetInventory()
    {
        return m_inventory.GetInventory();
    }

    public bool IsInstallTarget()
    {
        return true;
    }
    
}
