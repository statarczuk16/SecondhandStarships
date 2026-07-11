using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Component_Ship : MonoBehaviour
{
    [SerializeField] private Data_Ship m_data;
    [SerializeField] public GameObject m_ship_prefab; // prefab with PartComponents + fasteners pre-wired
    [SerializeField] List<Component_ShipPart> m_parts;

    void Awake() => m_parts = GetComponentsInChildren<Component_ShipPart>().ToList();

    

    internal void Update()
    {
        foreach(Component_ShipPart part in m_parts)
        {
            //idk do something
        }
    }

    internal void InstallPart(Component_ShipPart part)
    {
        if(m_parts.Contains(part))
        {
            TopicLogger.Log(LogTopic.Installation, LogLevel.ERROR, $"Ship {this.name} trying to install part already installed {part.name}");
            return;
        }
        m_parts.Add(part);
    }

    internal void OnPartUninstalled(Component_ShipPart part)
    {
        if (!m_parts.Contains(part))
        {
            TopicLogger.Log(LogTopic.Installation, LogLevel.ERROR, $"Ship {this.name} trying to uninstall part it doesnt have installed {part.name}");
            return;
        }
        m_parts.Remove(part);
    }
}

