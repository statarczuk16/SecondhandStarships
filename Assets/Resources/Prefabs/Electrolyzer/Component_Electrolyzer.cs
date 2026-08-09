using System;
using NUnit.Framework.Constraints;
using UnityEngine;

public class Component_Electrolyzer : MonoBehaviour, IToggleable, IInventoryOwner 
{
    [SerializeField] private Component_Inventory m_inventory;
    [SerializeField] private Component_Converter m_converter;
    private bool m_is_running = false;
    private AudioHandle m_electrolyzer_sound_handle = AudioHandle.Invalid;

    private void Update()
    {
        if (IsOn())
        {
            if (CanStayOn() == false)
            {
                Toggle(); //turn off if inventory lacks stuff it needs to function
            }
        }
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

    public bool CanStayOn()
    {
        return !IsOn() || CanToggle(out string reason);
    }

    public void Toggle()
    {
        if (IsOn()) //if we are on and the audio is playing, turn audio off
        {
            if (this.m_electrolyzer_sound_handle.IsValid)
            {
                 AudioEvents.StopLoop(this.m_electrolyzer_sound_handle);
                 this.m_electrolyzer_sound_handle = AudioHandle.Invalid;
            }
            m_converter.TurnOff();
           
        }
        else//if we are off and the audio is not playing, turn audio on
        {
            if (!this.m_electrolyzer_sound_handle.IsValid)
            {
                this.m_electrolyzer_sound_handle = AudioEvents.StartLoop(SoundID.Generator, this.transform);
            }
            m_converter.TurnOn();
        }
        m_is_running = !m_is_running;
    }

    public bool IsOn()
    {
        return m_is_running;
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
