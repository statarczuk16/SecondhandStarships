using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class Data_PowerNode
{
    [SerializeField] public LeakSeverity m_leak_severity = LeakSeverity.NONE;
    public const float BROKEN_CAPACITY_FRACTION = 1f;//we leak all our capacity
}
public class Component_PowerNode : MonoBehaviour, IMountable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private List<Component_PowerNode> m_connections = new List<Component_PowerNode>();
    [SerializeField] private Component_PowerNetwork m_owning_network;
    
    public void AddBidirectional(GameObject mountable)
    {
        Component_PowerNode node = mountable.GetComponentInChildren<Component_PowerNode>();
        if(m_connections.Contains(node) == false)
        {
            m_connections.Add(node);
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
    
    public void ClearOwner()
    {
        m_owning_network = null;
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

    public float DrawPower(float requested_power)
    {
        return this.m_owning_network.RequestPower(requested_power);
    }
    
    public bool CanDrawPower(float requested_power)
    {
        return this.m_owning_network?.GetAvailablePower() >= requested_power;
    }

    public bool HasPower()
    {
        if (this.m_owning_network == null)
        {
            return false;
        }
        return this.m_owning_network.CheckHasPower();
    }

    internal void BuildNetwork(Component_PowerNetwork network)
    {
        network.AddToNetwork(this);

        m_connections.Clear();

        //There are two ways a PowerNode can be connected to another PowerNode
        //1. If there is another power node connected to one of my relays
        //
        //    <-[ me ]-><-[other node]->
        //
        //2. If I am connected to another power node through a relay
        //
        // <-[ me ]-><-[ relay ]-><-[ other node ]->
        //
        //(relays can also chain: mount -> relay -> mount -> relay -> mount -> node)
        //so we walk out from all of our own mount points, hopping through any relays
        //we find, until we hit a mount owned by another power node.

        Component_MountPoint[] my_mounts = this.GetComponentsInChildren<Component_MountPoint>();
        List<Component_PowerNode> neighbors = Utility_PowerTraversal.FindNeighborNodes(my_mounts);

        foreach (Component_PowerNode other_node in neighbors)
        {
            if (other_node != this && m_connections.Contains(other_node) == false)
            {
                m_connections.Add(other_node);
            }
        }

        foreach(var node in m_connections)
        {
            if(network.CheckNodeInNetwork(node) == false)
            {
                node.BuildNetwork(network);
            }    
        }
    }
        
    private void OnDestroy()
    {
        if (m_owning_network == null)
        {
            return;
        }

        try
        {
            m_owning_network.BuildNetwork();
        }
        catch (MissingReferenceException)
        {
            // Owning network is being destroyed as part of the same operation
            // that destroyed us (e.g. a whole section going away together) -
            // there's nothing left to rebuild.
        }
    }

    public void ConnectGenerator(IPowerGenerator g)
    {
       this.m_owning_network?.ConnectGenerator(g);
    }

    public void DisconnectGenerator(IPowerGenerator g)
    {
        this.m_owning_network?.DisconnectGenerator(g);
    }

    public void ConnectConsumer(IPowerConsumer c)
    {
        this.m_owning_network?.ConnectConsumer(c);
    }
    
    public void DisconnectConsumer(IPowerConsumer c)
    {
        this.m_owning_network?.DisconnectConsumer(c);
    }

    public void ConnectCapacitor(IPowerCapacity c)
    {
        this.m_owning_network?.ConnectCapacitor(c);
    }

    public void DisconnectCapacitor(IPowerCapacity c)
    {
        this.m_owning_network?.DisconnectCapacitor(c);
    }
}
