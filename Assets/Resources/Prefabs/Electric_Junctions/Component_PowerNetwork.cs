using System.Collections.Generic;
using UnityEngine;

public class Component_PowerNetwork : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<Component_PowerNode> m_network_nodes = new List<Component_PowerNode>();
    //if we find another network during building our network, save it here
    [SerializeField] private List<Component_PowerNetwork> m_preempted_networks = new List<Component_PowerNetwork>();
    void Start()
    {
        BuildNetwork();
    }

    public void AddToNetwork(Component_PowerNode node)
    {
        if (m_network_nodes.Contains(node) == false)
        {
            m_network_nodes.Add(node);
            node.SetOwner(this);
        }
    }

    public bool CheckNodeInNetwork(Component_PowerNode node)
    {
        if(m_network_nodes.Contains(node))
        {
            return true;
        }
        return false;
    }
    
    public void RegisterPreemptedNetwork(Component_PowerNetwork preempted)
    {
        if (preempted != this && m_preempted_networks.Contains(preempted) == false)
        {
            m_preempted_networks.Add(preempted);
        }
    }
    
    public void BuildNetwork()
    {
        foreach (Component_PowerNode node in m_network_nodes)
        {
            if (node != null)
            {
                node.ClearOwner();
            }
        }
        m_network_nodes.Clear();

        List<Component_PowerNetwork> to_notify = new List<Component_PowerNetwork>(m_preempted_networks);
        m_preempted_networks.Clear();

        Component_PowerNode[] seed_nodes = GetComponentsInChildren<Component_PowerNode>();

        foreach (Component_PowerNode node in seed_nodes)
        {
            Component_PowerNetwork existing_owner = node.GetOwningNetwork();
            if (existing_owner == null)
            {
                node.BuildNetwork(this);
            }
            else if (existing_owner != this)
            {
                existing_owner.RegisterPreemptedNetwork(this);
            }
        }

        TopicLogger.Log(LogTopic.PowerSystem, LogLevel.INFO, $"TraverseNetwork found {m_network_nodes.Count} nodes");

        foreach (Component_PowerNetwork network in to_notify)
        {
            if (network != null)
            {
                network.BuildNetwork();
            }
        }
    }



}
