using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Component_Door : MonoBehaviour, IInteractable, IToggleable, IInventoryOwner
{
    [SerializeField, Required] private List<DOTweenAnimation> doorAnimations;
    [SerializeField, Required] private Component_Inventory m_inventory;
    [SerializeField, Required] private Component_Health m_health;
    [SerializeField, Required] private Component_PowerConsumer m_power_consumer;
    [SerializeField] private List<GameObject> m_destroy_on_death;
    private bool m_door_open;
    private bool m_has_power = false;
    private Action m_onLostPower;
    private Action m_onGainedPower;

    public void Awake()
    {
        this.m_inventory.ClaimInventory(this);
    }
    
   
    
    private void OnEnable()
    {
        if (m_health != null)
            m_health.OnDestroyed += HandleDestruction;
        if (m_power_consumer == null)
            return;
        
        m_onLostPower = () =>
        {
            m_has_power = false;
        };

        m_onGainedPower = () =>
        {
            m_has_power = true;
        };

        m_power_consumer.OnLostPower += m_onLostPower;
        m_power_consumer.OnGainedPower += m_onGainedPower;
        m_power_consumer.SetPassiveConsumptionOn(false);
    }

    private void OnDisable()
    {
        if (m_health != null)
            m_health.OnDestroyed -= HandleDestruction;
        m_power_consumer.OnLostPower -= m_onLostPower;
        m_power_consumer.OnGainedPower -= m_onGainedPower;
    }
    
    private void HandleDestruction()
    {
        foreach (GameObject obj in m_destroy_on_death)
        {
            obj.SetActive(false);
        }

        this.m_inventory.ClearAll();
    }
    
    public bool OnRequirementsMet(out string reason)
    {
        reason = "Whatever";
        if (this.m_health.IsDestroyed)
        {
            reason = "//ERROR: DOOR IS DESTROYED";
            return false;
        }
        if (!this.GetInventory().HasAllRecipeItems())
        {
            reason = "//ERROR: DOOR MALFUNCTION > CHECK SERVICE HATCH";
            return false;
        }


        if (this.m_power_consumer.GetAcivationPower() > 0)
        {
            if (!m_has_power)
            {
                reason = "//ERROR: NO POWER TO DOOR";
                return false;
            }
        }

        if (!(this.m_power_consumer.GetAvailablePower() >= this.m_power_consumer.GetAcivationPower()))
        {
            float available = this.m_power_consumer.GetAvailablePower();
            reason = $"//ERROR: POWER NETWORK INSUFFICIENT (NEED {this.m_power_consumer.GetAcivationPower()} UNITS) BUT (NETWORK HAS: {available} UNITS)";
            return false;
        }
        
        return true;
    }
    
    
    public void ToggleWantsToBeOn()
    {
        if (!OnRequirementsMet(out _))
        {
            return; //can't toggle, requirements not met
        }

        if (IsOn()) TurnOff();
        else TurnOn();
    }

    public bool IsOn()
    {
        return WantsToBeOn();
    }

    public void TurnOn()
    {
        float power_needed_to_open = m_power_consumer.GetAcivationPower();
        float power_drawn = m_power_consumer.TryDrawPower(power_needed_to_open);
        if (power_drawn < power_needed_to_open)
        {
            Debug.Log("!!!! Not enough power to open door. TODO replace with sound effect");
            return;
        }
        m_door_open = true;
        AudioEvents.Fire(SoundID.DoorOpen, transform.position);
        foreach (var animation in doorAnimations)
        {
            animation.DOPlayForward();
        }
    }

    public void TurnOff()
    {
        m_door_open = false;
        AudioEvents.Fire(SoundID.DoorClose, transform.position);
        foreach (var animation in doorAnimations)
        {
            animation.DOPlayBackwards();
        }
    }
    
    public bool WantsToBeOn()
    {
        return m_door_open; //door has no separate switch state — wants-to-be-on and is-on are the same thing
    }


    public bool CanToggle(out string reason)
    {
        return OnRequirementsMet(out reason);
    }
    
    public bool CanInteract(Controller_Equipment controller)
    {
        string raeson;
        return CanToggle(out raeson);
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        
    }

    public void OnHoverExit(Controller_Equipment controller)
    {
        
    }

    public void OnInteract(Controller_Equipment controller)
    {
        if (CanToggle(out string reason))
        {
            this.ToggleWantsToBeOn();
        }
        
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController, RaycastHit hitInfo)
    {
        if (equipmentController.TorchMode())
        {
            float heat_from_torch = equipmentController.GenerateHeat(Time.deltaTime);
            GetComponent<Component_Temperature>()?.AddHeat(heat_from_torch);
        }
    }

    public string GetInteractionLabel(Controller_Equipment controller)
    {
        string reason = "";
        if (CanToggle(out reason))
        {
            if (m_door_open)
            {
                return "Close Door";
            }
            else
            {
                return "Open Door";
            }
        }
        else
        {
            return reason;
        }
        
        
    }

    public Transform InteractionPoint { get; }
    public Data_Inventory GetInventory()
    {
        return this.m_inventory.GetInventory();
    }

    public bool IsInstallTarget()
    {
        return true;
    }
    
}