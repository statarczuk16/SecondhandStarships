using System.Collections.Generic;
using UnityEngine;

public class Component_ShipChunk : MonoBehaviour
{
    [SerializeField, Required] public Component_Ship m_owning_ship;
    [SerializeField] List<Component_ShipPart> m_parts;
    
    public void InstallToShip(Component_Ship ship)
    {
        m_owning_ship = ship;
        var surfaces = GetComponentsInChildren<Component_BuildableSurface>();
        foreach (Component_BuildableSurface surface in surfaces)
        {
            surface.InstallToChunk(this);
        }
    }

    public void OnPartUninstalled(Component_ShipPart part)
    {
        if (!m_parts.Contains(part))
        {
            TopicLogger.Log(LogTopic.Installation, LogLevel.ERROR, $"Chunk {this.name} trying to uninstall part it doesnt have installed {part.name}");
            return;
        }
        m_parts.Remove(part);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    public void InstallPart(Component_ShipPart part)
    {
        if(m_parts.Contains(part))
        {
            TopicLogger.Log(LogTopic.Installation, LogLevel.ERROR, $"Chunk {this.name} trying to install part already installed {part.name}");
            return;
        }
        m_parts.Add(part);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (m_owning_ship?.m_chunks.Contains(this) == false)
        {
            Debug.LogError($"Chunk {this.name} and {m_owning_ship.name} reminding ship that it owns this chunk");
            m_owning_ship.InstallChunk(this);
        }
#endif
    }
    
   
}
