using System;
using NUnit.Framework.Constraints;
using UnityEngine;

public class Component_Generator : MonoBehaviour, IToggleable, IInventoryOwner, IPowerGenerator
{
    [SerializeField] private Component_Inventory m_inventory;
    private bool m_is_running = false;
    private AudioHandle m_generator_sound_handle = AudioHandle.Invalid;
    private const int POWER_PER_SECOND = 1000;

    public void Awake()
    {
        this.m_inventory.ClaimInventory(this);
    }
    private void Update()
    {
        if (IsOn())
        {
            if (CanStayOn() == false)
            {
                Toggle(); //turn generator off if inventory lacks stuff it needs to function
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
            if (this.m_generator_sound_handle.IsValid)
            {
                 AudioEvents.StopLoop(this.m_generator_sound_handle);
                 this.m_generator_sound_handle = AudioHandle.Invalid;
            }
           
        }
        else//if we are off and the audio is not playing, turn audio on
        {
            if (!this.m_generator_sound_handle.IsValid)
            {
                this.m_generator_sound_handle = AudioEvents.StartLoop(SoundID.Generator, this.transform);
            }
           
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

    public float GetPowerGeneratedThisDT()
    {
        if(IsOn())
        {
            return this.GetPowerGeneratedThisDT() * Time.deltaTime;
        }
        return 0f;
    }
}
