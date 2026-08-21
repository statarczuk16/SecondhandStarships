using System;
using System.Collections.Generic;
using UnityEngine;

public class Component_Relay : MonoBehaviour
{
    [SerializeField] public List<Component_MountPoint> m_connected_mounts;


    private void Awake()
    {
        if (m_connected_mounts == null)
        {
            m_connected_mounts = new List<Component_MountPoint>();
        }
    }

    private void OnDestroy()
    {
        // Before we sever anything: find every power node reachable through us
        // (possibly through further relay hops) and rebuild each one's owning
        // network, since connectivity that depended on us is about to change.
        HashSet<Component_Relay> visited = new HashSet<Component_Relay> { this };
        List<Component_PowerNode> affected_nodes =
            Utility_PowerTraversal.FindNeighborNodes(m_connected_mounts, visited);

        HashSet<Component_PowerNetwork> notified = new HashSet<Component_PowerNetwork>();
        foreach (Component_PowerNode node in affected_nodes)
        {
            Component_PowerNetwork owner = node.GetOwningNetwork();
            if (owner != null && notified.Add(owner))
            {
                owner.BuildNetwork();
                break;
            }
        }

        // Now clear the dangling references - the far side mount still thinks
        // it's connected to one of our (about-to-be-gone) mounts.
        foreach (Component_MountPoint mount in m_connected_mounts)
        {
            if (mount == null) continue;
            Component_MountPoint far = mount.ConnectedTo;
            mount.ClearConnection();
            if (far != null)
            {
                far.ClearConnection();
            }
        }
    }
}
