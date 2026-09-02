using System.Collections.Generic;

// Shared BFS used by both Component_PowerNode (finding my immediate node
// neighbors when building a network) and Component_Relay (finding who might
// be affected when I'm destroyed). Walks outward from a set of mount points,
// treating any relay encountered as a pass-through: arriving at one of a
// relay's mounts means every other mount on that relay is also reachable.
// A Component_PowerNode is treated as a terminal - we don't walk through it.
internal static class Utility_PowerTraversal
{
    public static List<Component_PowerNode> FindNeighborNodes(
        IEnumerable<Component_MountPoint> startMounts,
        HashSet<Component_Relay> visitedRelays = null)
    {
        visitedRelays ??= new HashSet<Component_Relay>();
        List<Component_PowerNode> found = new List<Component_PowerNode>();
        Queue<Component_MountPoint> frontier = new Queue<Component_MountPoint>(startMounts);

        while (frontier.Count > 0)
        {
            Component_MountPoint mount = frontier.Dequeue();
            if (mount == null) continue;

            Component_MountPoint connected = mount.ConnectedTo;
            if (connected == null) continue;

            Component_PowerNode node = connected.GetComponentInParent<Component_PowerNode>();
            if (node != null)
            {
                if (!found.Contains(node))
                {
                    found.Add(node);
                }
                continue; // nodes are terminal, don't walk through them
            }

            Component_Relay relay = connected.GetComponentInParent<Component_Relay>();
            if (relay != null && visitedRelays.Add(relay))
            {
                foreach (Component_MountPoint relay_mount in relay.m_connected_mounts)
                {
                    if (relay_mount != connected)
                    {
                        frontier.Enqueue(relay_mount);
                    }
                }
            }
        }

        return found;
    }
}