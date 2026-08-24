using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Component_Ship : MonoBehaviour
{
    [SerializeField] private Data_Ship m_data;
    [SerializeField] public GameObject m_ship_prefab; // prefab with PartComponents + fasteners pre-wired
    [SerializeField] public List<Component_ShipChunk> m_chunks;

    
    internal void Update()
    {
        
    }

    internal void InstallChunk(Component_ShipChunk chunk)
    {
        if(m_chunks.Contains(chunk))
        {
            TopicLogger.Log(LogTopic.Installation, LogLevel.ERROR, $"Ship {this.name} trying to install chunk already installed {chunk.name}");
            return;
        }
        m_chunks.Add(chunk);
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
        #endif 
    }

    internal void OnPartUninstalled(Component_ShipChunk chunk)
    {
        if (!m_chunks.Contains(chunk))
        {
            TopicLogger.Log(LogTopic.Installation, LogLevel.ERROR, $"Ship {this.name} trying to uninstall chunk it doesnt have installed {chunk.name}");
            return;
        }
        m_chunks.Remove(chunk);
        #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    


}

