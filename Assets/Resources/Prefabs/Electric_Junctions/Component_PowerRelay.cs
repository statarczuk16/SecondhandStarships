using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Data_PowerRelay
{
    [SerializeField] public LeakSeverity m_leak_severity = LeakSeverity.NONE;
    public const float BROKEN_CAPACITY_FRACTION = 1f;//we leak all our capacity
}
public class Component_PowerRelay : MonoBehaviour, IMountable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private List<Component_PowerRelay> m_connections;
    [SerializeField] private Component_PowerNetwork m_owning_network;
    
    public void AddBidirectional(GameObject mountable)
    {
        Component_PowerRelay relay = GetComponentInChildren<Component_PowerRelay>(mountable);
        if(m_connections.Contains(relay) == false)
        {
            m_connections.Add(relay);
        }    
    }

    public void AddDownStream(GameObject mountable)
    {
        throw new System.NotImplementedException();
    }

    public void AddUpstream(GameObject mountable)
    {
        throw new System.NotImplementedException();
    }

    public void SetOwner(Component_PowerNetwork network)
    {
        m_owning_network = network;
    }    

    public Component_PowerNetwork GetOwningNetwork()
    {
        if(m_owning_network == null)
        {
            return null;
        }
        if(this.m_owning_network.CheckNodeInNetwork(this))
        {
            return m_owning_network;
        }
        else
        {
            TopicLogger.Log(LogTopic.PowerSystem, LogLevel.WARN, $"Power_Relay {this.name} removed from {m_owning_network.name} without being told. Removing now.");
            m_owning_network = null;
        }
        return null;
    }

    internal void BuildNetwork(Component_PowerNetwork network)
    {
        
        network.AddToNetwork(this);

        foreach(var node in m_connections)
        {
            if(network.CheckNodeInNetwork(node) == false)
            {
                node.BuildNetwork(network);
            }    
        }
    }
}
