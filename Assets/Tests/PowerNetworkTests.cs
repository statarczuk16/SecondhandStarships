using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Edit Mode tests. Every BuildNetwork() call is invoked directly (no Start()),
// so ordering between networks is fully test-controlled rather than relying on
// Unity's script execution order.
public class PowerNetworkTests
{
    private readonly List<GameObject> m_created = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        m_created.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        
        foreach (GameObject go in m_created)
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
        m_created.Clear();
    }

    // ---- helpers ----------------------------------------------------------

    private GameObject NewObject(string name)
    {
        GameObject go = new GameObject(name);
        m_created.Add(go);
        return go;
    }

    private Component_PowerNode CreateNode(string name, Transform parent)
    {
        GameObject go = NewObject(name);
        if (parent != null)
        {
            go.transform.SetParent(parent);
        }
        return go.AddComponent<Component_PowerNode>();
    }

    private Component_MountPoint AddMount(GameObject go)
    {
        return go.AddComponent<Component_MountPoint>();
    }

    private Component_Relay CreateRelay(string name, out Component_MountPoint mount1, out Component_MountPoint mount2)
    {
        GameObject go = NewObject(name);
        Component_Relay relay = go.AddComponent<Component_Relay>();
        mount1 = AddMount(go);
        mount2 = AddMount(go);
        relay.m_connected_mounts = new List<Component_MountPoint> { mount1, mount2 };
        return relay;
    }

    // ---- tests --------------------------------------------------------------

    // 1. A network should discover nodes both through a direct mount-to-mount
    //    connection and through a relay hop, even when those nodes live
    //    completely outside the network's own transform hierarchy.
    [Test]
    public void BuildNetwork_FindsNodes_ViaDirectMountAndRelayChain()
    {
        GameObject networkGO = NewObject("Network");
        Component_PowerNetwork network = networkGO.AddComponent<Component_PowerNetwork>();

        // Only NodeA is actually parented under the network - it's the seed.
        Component_PowerNode nodeA = CreateNode("NodeA", networkGO.transform);
        Component_MountPoint mountA1 = AddMount(nodeA.gameObject); // -> direct to NodeB
        Component_MountPoint mountA2 = AddMount(nodeA.gameObject); // -> into relay

        // NodeB: reachable directly from NodeA, lives outside the network's
        // hierarchy entirely - only mount traversal should find it.
        Component_PowerNode nodeB = CreateNode("NodeB", null);
        Component_MountPoint mountB1 = AddMount(nodeB.gameObject);
        Component_MountPoint.ConnectMounts(mountA1, mountB1);

        // Relay bridging NodeA to NodeC.
        Component_Relay relay = CreateRelay("Relay", out Component_MountPoint mountR1, out Component_MountPoint mountR2);
        Component_MountPoint.ConnectMounts(mountA2, mountR1);

        Component_PowerNode nodeC = CreateNode("NodeC", null);
        Component_MountPoint mountC1 = AddMount(nodeC.gameObject);
        Component_MountPoint.ConnectMounts(mountR2, mountC1);

        network.BuildNetwork();

        Assert.AreEqual(3, network.m_network_nodes.Count);
        Assert.IsTrue(network.CheckNodeInNetwork(nodeA));
        Assert.IsTrue(network.CheckNodeInNetwork(nodeB));
        Assert.IsTrue(network.CheckNodeInNetwork(nodeC));
    }

    // 2. Two networks connected through a shared node graph: whichever builds
    //    first claims every reachable node, including the other network's own
    //    seed. The second network ends up owning nothing.
    [Test]
    public void BuildNetwork_FirstNetworkToRun_ClaimsEntireConnectedGraph()
    {
        GameObject networkAGO = NewObject("NetworkA");
        Component_PowerNetwork networkA = networkAGO.AddComponent<Component_PowerNetwork>();
        Component_PowerNode seedA = CreateNode("SeedA", networkAGO.transform);
        Component_MountPoint mountSA = AddMount(seedA.gameObject);

        GameObject networkBGO = NewObject("NetworkB");
        Component_PowerNetwork networkB = networkBGO.AddComponent<Component_PowerNetwork>();
        Component_PowerNode seedB = CreateNode("SeedB", networkBGO.transform);
        Component_MountPoint mountSB = AddMount(seedB.gameObject);

        Component_PowerNode bridge = CreateNode("Bridge", null);
        Component_MountPoint mountBridge1 = AddMount(bridge.gameObject);
        Component_MountPoint mountBridge2 = AddMount(bridge.gameObject);
        Component_MountPoint.ConnectMounts(mountSA, mountBridge1);
        Component_MountPoint.ConnectMounts(mountBridge2, mountSB);

        networkA.BuildNetwork(); // A runs first - should claim the whole graph
        networkB.BuildNetwork(); // B seeds into territory A already owns

        Assert.AreEqual(3, networkA.m_network_nodes.Count);
        Assert.IsTrue(networkA.CheckNodeInNetwork(seedA));
        Assert.IsTrue(networkA.CheckNodeInNetwork(bridge));
        Assert.IsTrue(networkA.CheckNodeInNetwork(seedB));

        Assert.AreEqual(0, networkB.m_network_nodes.Count);
    }

    // 3. Same setup as above, but after A claims everything, the bridging
    //    node is destroyed. A should rebuild down to just its own seed, and
    //    that rebuild should cascade to the preempted network B, letting it
    //    claim its own now-orphaned seed - ending in two independent, disjoint
    //    single-node networks.
    [Test]
    public void DestroyingBridgeNode_SplitsIntoTwoIndependentNetworks()
    {
        GameObject networkAGO = NewObject("NetworkA");
        Component_PowerNetwork networkA = networkAGO.AddComponent<Component_PowerNetwork>();
        Component_PowerNode seedA = CreateNode("SeedA", networkAGO.transform);
        Component_MountPoint mountSA = AddMount(seedA.gameObject);

        GameObject networkBGO = NewObject("NetworkB");
        Component_PowerNetwork networkB = networkBGO.AddComponent<Component_PowerNetwork>();
        Component_PowerNode seedB = CreateNode("SeedB", networkBGO.transform);
        Component_MountPoint mountSB = AddMount(seedB.gameObject);

        Component_PowerNode bridge = CreateNode("Bridge", null);
        Component_MountPoint mountBridge1 = AddMount(bridge.gameObject);
        Component_MountPoint mountBridge2 = AddMount(bridge.gameObject);
        Component_MountPoint.ConnectMounts(mountSA, mountBridge1);
        Component_MountPoint.ConnectMounts(mountBridge2, mountSB);

        networkA.BuildNetwork();
        networkB.BuildNetwork();

        // Sanity check on pre-split state before we tear the bridge down.
        Assert.AreEqual(3, networkA.m_network_nodes.Count);
        Assert.AreEqual(0, networkB.m_network_nodes.Count);

        Object.DestroyImmediate(bridge.gameObject);

        Assert.AreEqual(1, networkA.m_network_nodes.Count);
        Assert.IsTrue(networkA.CheckNodeInNetwork(seedA));

        Assert.AreEqual(1, networkB.m_network_nodes.Count);
        Assert.IsTrue(networkB.CheckNodeInNetwork(seedB));
    }
}