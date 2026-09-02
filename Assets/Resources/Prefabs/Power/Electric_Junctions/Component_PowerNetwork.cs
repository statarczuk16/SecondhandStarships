using System;
using System.Collections.Generic;
using UnityEngine;

public class Component_PowerNetwork : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<Component_PowerNode> m_network_nodes = new List<Component_PowerNode>();
    //if we find another network during building our network, save it here
    [SerializeField] private List<Component_PowerNetwork> m_preempted_networks = new List<Component_PowerNetwork>();
    [SerializeField] private List<IPowerGenerator> m_generators = new List<IPowerGenerator>();
    [SerializeField] private List<IPowerCapacity> m_capacitors = new List<IPowerCapacity>();
    [SerializeField] private List<IPowerConsumer> m_consumers = new List<IPowerConsumer>();
    [SerializeField] private float m_power_capacity = 0f;
    [SerializeField] private float m_stored_power = 0f;
    [SerializeField] private float m_power_generated_this_tic = 0f;
    [SerializeField] private float m_total_power_consumption = 0f;
    [SerializeField] private float m_avg_power_consumption_per_s = 0f;

    [SerializeField] private float m_total_runtime = 0f;
    public static float POWER_UPDATE_TIC_s = .25f;
    private float update_tic_counter_s = 0f;
    void Start()
    {
        BuildNetwork();
    }

    private void Update()
    {
        update_tic_counter_s += Time.deltaTime;
        if (update_tic_counter_s < POWER_UPDATE_TIC_s)
        {
            return;
        }
        
        //At start of tick, stored power can't be more than stored power capacity (IE, we lost a battery last tick)
        m_stored_power = Mathf.Clamp(m_stored_power, 0f, m_power_capacity);
        m_power_generated_this_tic = 0f;
        
        foreach (IPowerGenerator generator in m_generators)
        {
            m_power_generated_this_tic += generator.GetPowerGeneratedPerDT(update_tic_counter_s);
        }

        /**
       //consume power for each power consumer. if any component cant get enough, we tell the consumer theres no power
        m_power_consumption_per_tic = 0f;
       float available_power = m_power_generated_this_tic + m_stored_power;
    
       foreach (IPowerConsumer consumer in m_consumers)
       {
           float power_needed = consumer.PowerConsumedPerDT(update_tic_counter_s);
           m_power_consumption_per_tic += power_needed;
           if (available_power < power_needed)
           {
               consumer.PowerUpdate(true);
           }
           else
           {
               consumer.PowerUpdate(false);
           }
           available_power -= power_needed;
           if (available_power <= 0)
           {
               available_power = 0;
           }
       }
      
        //store whatever is left over in the batteries (or drain them)
        
         **/

        RebalancePower(m_stored_power + m_power_generated_this_tic);
        m_total_runtime += update_tic_counter_s;
        m_avg_power_consumption_per_s = m_total_power_consumption /  m_total_runtime;
        update_tic_counter_s = 0f;
        
    }
    
    private void RebalancePower(float total_available_power)
    {
        m_stored_power = Mathf.Clamp(
            total_available_power,
            0f,
            m_power_capacity
        );

        m_power_generated_this_tic = Mathf.Max(
            0f,
            total_available_power - m_stored_power
        );
    }

    public float RequestPower(float requested_power)
    {
        float total_available_power = m_stored_power + m_power_generated_this_tic;

        float power_given = Mathf.Min(
            requested_power,
            total_available_power
        );

        total_available_power -= power_given;
        m_total_power_consumption += power_given;

        RebalancePower(total_available_power);

        return power_given;
    }

    public bool CheckHasPower()
    {
        return m_stored_power + m_power_generated_this_tic > 0f;
    }
    
    public float GetAvailablePower()
    {
        return m_stored_power + m_power_generated_this_tic;
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
        m_capacitors.Clear();
        m_power_capacity = 0;
        m_generators.Clear();
        m_consumers.Clear();

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
    
    public override string ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine($"=== Power Network: {gameObject.name} ===");
        sb.AppendLine($"Nodes: {m_network_nodes.Count}");
        sb.AppendLine($"Generators: {m_generators.Count}");
        sb.AppendLine($"Capacitors: {m_capacitors.Count}");
        sb.AppendLine($"Power Capacity: {m_power_capacity:F2}");
        sb.AppendLine($"Stored Power: {m_stored_power:F2}");
        sb.AppendLine($"Power Generation/Tic: {m_power_generated_this_tic:F2}");
        sb.AppendLine($"Power Consumed/Tic: {m_avg_power_consumption_per_s:F2}");
        sb.AppendLine($"Power Utilization: " +
                      $"{(m_avg_power_consumption_per_s > 0f ? (m_avg_power_consumption_per_s / m_power_generated_this_tic) * 100f : 0f):F1}%");

        sb.AppendLine();
        sb.AppendLine($"Connected Nodes {m_network_nodes.Count}");

        sb.AppendLine();
        sb.AppendLine("--- Generators ---");

        foreach (IPowerGenerator generator in m_generators)
        {
            if (generator == null)
            {
                sb.AppendLine("  [NULL GENERATOR]");
                continue;
            }

            if (generator is Component component)
            {
                sb.AppendLine($"  {component.gameObject.name}");
            }
            else
            {
                sb.AppendLine($"  {generator.GetType().Name}");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("--- Consumers ---");

        foreach (IPowerConsumer consumer in m_consumers)
        {
            if (consumer == null)
            {
                sb.AppendLine("  [NULL CONSUMER]");
                continue;
            }

            string temp_name = "Unknown";
            string temp_on = "";
            string temp_power_per_sec = "0 needed/second";
            if (consumer is Component component)
            {
                temp_name = component.gameObject.name;
            }
           
            temp_power_per_sec = $"{consumer.PowerConsumedPerDT(1):F2} needed/second";
            if (consumer is IToggleable toggleable)
            {
                temp_on = $"SWITCHED: {(toggleable.WantsToBeOn() ? "ON" : "OFF")} " +
                          $"IS_ON: {(toggleable.IsOn() ? "TRUE" : "FALSE")}";
            }
            sb.AppendLine($"{temp_name} {temp_on} {temp_power_per_sec}");
        }

        sb.AppendLine();
        sb.AppendLine("--- Capacitors ---");

        foreach (IPowerCapacity capacitor in m_capacitors)
        {
            if (capacitor == null)
            {
                sb.AppendLine("  [NULL CAPACITOR]");
                continue;
            }

            if (capacitor is Component component)
            {
                sb.AppendLine(
                    $"  {component.gameObject.name}: " +
                    $"{capacitor.GetPowerCapacity():F2} capacity");
            }
            else
            {
                sb.AppendLine(
                    $"  {capacitor.GetType().Name}: " +
                    $"{capacitor.GetPowerCapacity():F2} capacity");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Preempted Networks: {m_preempted_networks.Count}");

        return sb.ToString();
    }


    public void ConnectGenerator(IPowerGenerator g)
    {
        if (this.m_generators.Contains(g) == false)
        {
            this.m_generators.Add(g);
        }
    }

    public void DisconnectGenerator(IPowerGenerator g)
    {
        this.m_generators.Remove(g);
    }

    public void ConnectConsumer(IPowerConsumer p)
    {
        this.m_consumers.Add(p);
    }
    
    public void DisconnectConsumer(IPowerConsumer p)
    {
        this.m_consumers.Remove(p);
    }

    public void ConnectCapacitor(IPowerCapacity p)
    {
        this.m_capacitors.Add(p);
        this.m_power_capacity +=  p.GetPowerCapacity();
    }

    public void DisconnectCapacitor(IPowerCapacity p)
    {
        this.m_capacitors.Remove(p);
        this.m_power_capacity -=  p.GetPowerCapacity();
        if (this.m_power_capacity < 0)
        {
            this.m_power_capacity = 0;
        }
    }
}
