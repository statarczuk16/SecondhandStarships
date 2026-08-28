using System;
using UnityEngine;

public class Component_PowerConsumer : MonoBehaviour, IPowerConsumer, IPowerNetworked
{
    [SerializeField, Required] private GameObject m_power_slot;
    [SerializeField] private Component_PowerNode m_connected_power_node;
    [SerializeField] private float m_power_radius;
    [SerializeField] private float m_power_usage_per_second;
    [SerializeField] private float m_power_usage_per_activation;
    private bool m_received_power_this_tic = false;
    private float update_tic_counter_s = 0f;
    private bool passive_consumption_on;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public event Action OnLostPower;
    public event Action OnGainedPower;
    
    public Component_PowerNode TryFindNodeWithPower()
    {
        var colliders = Physics.OverlapSphere(this.m_power_slot.transform.position, this.m_power_radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
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
        return false;
    }

    public float GetAcivationPower()
    {
        return m_power_usage_per_activation;
    }

    public float GetAvailablePower()
    {
        if (m_connected_power_node == null)
        {
            return 0;
        }
        else
        {
            return m_connected_power_node.GetOwningNetwork()?.GetAvailablePower() ?? 0f;
        }
    }

    private void Update()
    {
        update_tic_counter_s += Time.deltaTime;
        if (update_tic_counter_s < Component_PowerNetwork.POWER_UPDATE_TIC_s)
        {
            return;
        }

        float time_elapsed = update_tic_counter_s;
        update_tic_counter_s = 0f;
        if(this.m_connected_power_node == null)
        {
            Component_PowerNode temp = TryFindNodeWithPower();
            if (temp != null)
            {
                this.ConnectToNode(temp);
            }
            
        }
        if(this.m_connected_power_node == null)
        {
            OnLostPower?.Invoke();
            return;
        }

        float power_needed =  PowerConsumedPerDT(time_elapsed);
        float power_received = TryDrawPower(power_needed);
        if (power_received >= power_needed)
        {
            OnGainedPower?.Invoke();
        }
        else
        {
            OnLostPower?.Invoke();
        }
    }

    public float TryDrawPower(float power)
    {
        float power_received = this.m_connected_power_node?.DrawPower(power) ?? 0;
        return power_received;
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

    public float GetPowerRadius_M()
    {
        return m_power_radius;
    }

    public float PowerConsumedPerDT(float dt)
    {
        if (!PassiveConsumptionOn())
        {
            return 0f;
        }
        return this.m_power_usage_per_second * dt;
    }

    public bool PassiveConsumptionOn()
    {
        return passive_consumption_on;
    }

    public void SetPassiveConsumptionOn(bool on)
    {
        passive_consumption_on = on;
    }

    public float PowerNeededPerUsage()
    {
        return m_power_usage_per_activation;
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
