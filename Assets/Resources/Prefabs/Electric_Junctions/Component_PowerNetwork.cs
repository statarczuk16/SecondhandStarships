using System.Collections.Generic;
using UnityEngine;

public class Component_PowerNetwork : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<Component_PowerRelay> m_network_nodes;
    void Start()
    {
        BuildNetwork();
    }

    public void AddToNetwork(Component_PowerRelay node)
    {
        if (m_network_nodes.Contains(node) == false)
        {
            m_network_nodes.Add(node);
            node.SetOwner(this);
        }
    }

    public bool CheckNodeInNetwork(Component_PowerRelay node)
    {
        if(m_network_nodes.Contains(node))
        {
            return true;
        }
        return false;
    }

    void BuildNetwork()
    {
        m_network_nodes.Clear();
        Component_PowerRelay[] seed_nodes = GetComponentsInChildren<Component_PowerRelay>();
        
        foreach(Component_PowerRelay node in seed_nodes)
        {
            if(node.GetOwningNetwork() == null) //if another network already owns this power relay, this network component doesnt make its own network.
            {
                node.BuildNetwork(this);
            }
           
        }
        TopicLogger.Log(LogTopic.PowerSystem, LogLevel.INFO, $"TraverseNetwork found {m_network_nodes.Count} nodes");
    }



}
