using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Component_Door : MonoBehaviour, IInteractable, IToggleable, IInventoryOwner, IPowerConsumer, IPowerNetworked
{
    [SerializeField, Required] private List<DOTweenAnimation> doorAnimations;
    [SerializeField, Required] private Component_Inventory m_inventory;
    [SerializeField, Required] private GameObject m_power_slot;
    [SerializeField, Required] private Component_Health m_health;
    [SerializeField] private Component_PowerNode m_connected_power_node;
    [SerializeField] private float m_power_radius = 4;
    [SerializeField] private float m_power_usage = 1;
    [SerializeField] private List<GameObject> m_destroy_on_death;
    private bool m_is_on;
    private bool m_needs_startup_register = true;

    public void Awake()
    {
        this.m_inventory.ClaimInventory(this);
    }
    
    private void Update()
    {
        if (m_needs_startup_register)
        {
            m_needs_startup_register = TryGameStartNetworkConnect();
        }
    }
    
    private void OnEnable()
    {
        if (m_health != null)
            m_health.OnDestroyed += HandleDestruction;
    }

    private void OnDisable()
    {
        if (m_health != null)
            m_health.OnDestroyed -= HandleDestruction;
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

        if (!CheckHasPower())
        {
            reason = "//ERROR: NO POWER TO DOOR";
            return false;
        }

        if (!this.m_connected_power_node.CanDrawPower(m_power_usage))
        {
            float available = this.m_connected_power_node.GetOwningNetwork().GetAvailablePower();
            reason = $"//ERROR: POWER NETWORK INSUFFICIENT (NEED {this.m_power_usage} UNITS) BUT (NETWORK HAS: {available} UNITS)";
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
    
    public void TurnOn()
    {
        m_is_on = true;
        this.m_connected_power_node.DrawPower(this.m_power_usage);
        AudioEvents.Fire(SoundID.DoorOpen, transform.position);
        foreach (var animation in doorAnimations)
        {
            animation.DOPlayForward();
        }
    }

    public void TurnOff()
    {
        m_is_on = false;
        AudioEvents.Fire(SoundID.DoorClose, transform.position);
        foreach (var animation in doorAnimations)
        {
            animation.DOPlayBackwards();
        }
    }

    public bool IsOn()
    {
        return m_is_on;
    }

    public bool WantsToBeOn()
    {
        return m_is_on; //door has no separate switch state — wants-to-be-on and is-on are the same thing
    }


    public bool CanToggle(out string reason)
    {
        return OnRequirementsMet(out reason);
    }
    
    

    public bool CanInteract(Controller_Equipment controller)
    {
        return true;
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
            GetComponent<Component_Temperature>()?.AddHeat(equipmentController.GetHeatPerSecond() * Time.deltaTime);
        }
    }

    public string GetInteractionLabel(Controller_Equipment controller)
    {
        if (m_is_on)
        {
            return "Close Door";
        }
        else
        {
            return "Open Door";
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

    public Component_PowerNode TryFindPowerNode()
    {
        var colliders = Physics.OverlapSphere(this.m_power_slot.transform.position, this.GetPowerRadius_M(), Physics.AllLayers, QueryTriggerInteraction.Ignore);
        foreach (var collider in colliders)
        {
            if (collider.GetComponentInParent<Component_PowerNode>() != null)
            {
                Component_PowerNode component_PowerNode = collider.GetComponentInParent<Component_PowerNode>();
                if (component_PowerNode.GetOwningNetwork() && component_PowerNode.GetOwningNetwork().CheckHasPower())
                {
                    return component_PowerNode;
                }
            }
        }
        return null;
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
        if (m_connected_power_node != null)
        {
            DisconnectFromNode();
        }
        m_connected_power_node = node;
        node.ConnectConsumer(this);
    }

    public void DisconnectFromNode()
    {
        m_connected_power_node.DisconnectConsumer(this);
        m_connected_power_node = null;
    }
    
    public float PowerConsumedPerDT(float dt)
    {
        return 0f;
    }

    public bool CheckHasPower()
    {
        if (this.m_connected_power_node == null)
        {
            this.m_connected_power_node = this.TryFindPowerNode();
        }

        if (this.m_connected_power_node == null)
        {
            return false;
        }

        return this.m_connected_power_node.GetOwningNetwork().CheckHasPower();
    }

    public void SetPoweredStarved()
    {
        
    }

    public void SetHasPower()
    {
        
    }

    public float GetPowerRadius_M()
    {
        return m_power_radius;
    }

    public float PowerNeededPerUsage()
    {
        return m_power_usage;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Color color = Color.yellow;
        color.a = 0.05f;
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, this.m_power_radius);
        
    }

    
#endif
}