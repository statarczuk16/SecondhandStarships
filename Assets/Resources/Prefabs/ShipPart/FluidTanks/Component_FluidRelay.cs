using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum LeakSeverity
{
    NONE,
    LEAK,
    BROKEN
}

public enum PortType
{
    NONE,
    SEND,
    RECEIVE
}

[System.Serializable]
public class Data_FluidRelay
{
    [SerializeField] public LeakSeverity m_leak_severity = LeakSeverity.NONE;
    [SerializeField] public float m_leak_flat_L_s = 0.5f;
    [SerializeField] public float m_broken_flat_L_s = 3f;
    [SerializeField] public PortType mPortType = PortType.NONE;

    public const float LEAK_CAPACITY_FRACTION = 0.5f;//we leak half our capacity 
    public const float BROKEN_CAPACITY_FRACTION = 1f;//we leak all our capacity
}

[RequireComponent(typeof(Component_PrefabBoundary))]
public class Component_FluidRelay : MonoBehaviour, IFluidReceiver, IFluidSender
{
    // Note: Unless using Odin Inspector, Unity does not natively serialize interfaces. 
    // These will be populated via Awake / AddDownstream at runtime.
    [SerializeField] private List<IFluidReceiver> m_downstreams; 
    [SerializeField] private IFluidReceiver m_downstream_leak_target; 
    [SerializeField] private Data_FluidRelay m_data; 
    

    // Cache list to eliminate garbage collection in Update loops
    private List<float> m_cached_capacities = new List<float>();

    private void Awake()
    {
        // Only initialize if null to preserve any injected dependencies
        m_downstreams ??= new List<IFluidReceiver>();
        if (m_data == null)
        {
            m_data = new Data_FluidRelay();
        }
    }

    internal void AddDownstream(IFluidReceiver source)
    {
        if (!m_downstreams.Contains(source))
        {
            m_downstreams.Add(source);
            
        }
    }

    private float GetFlatLeakRatePerS() => m_data.m_leak_severity switch
    {
        LeakSeverity.LEAK => m_data.m_leak_flat_L_s,
        LeakSeverity.BROKEN => m_data.m_broken_flat_L_s,
        _ => 0f
    };

    private float GetCapacityLeakFraction() => m_data.m_leak_severity switch
    {
        LeakSeverity.LEAK => Data_FluidRelay.LEAK_CAPACITY_FRACTION,
        LeakSeverity.BROKEN => Data_FluidRelay.BROKEN_CAPACITY_FRACTION,
        _ => 0f
    };

    public float GetRemainingCapacityLitersThisDT(float dt, FluidType fluid)
    {
        // 1. If the pipe is severed, it acts as a dead-end vent.
        // It ignores downstream entirely, and its capacity is purely the size of the break.
        if (m_downstreams.Count == 0)
        {
            this.m_data.m_leak_severity = LeakSeverity.BROKEN;
        }
        if (m_data.m_leak_severity == LeakSeverity.BROKEN)
        {
            return GetFlatLeakRatePerS() * dt;
        }

        // 2. If it's a partial leak or perfectly fine, it accepts what fits 
        // through the hole PLUS what fits into the downstream tanks.
        float downstream_total = 0f;
        
        // Zero-allocation loop
        for (int i = 0; i < m_downstreams.Count; i++)
        {
            downstream_total += m_downstreams[i].GetRemainingCapacityLitersThisDT(dt, fluid);
        }

        return downstream_total + (GetFlatLeakRatePerS() * dt);
    }

    public float ReceiveFluid(float amountL, float dt, FluidType type)
    {
        TopicLogger.Log(LogTopic.FluidSystem, LogLevel.INFO, $"{this.name} got {amountL}L of {type}!");

        float distributable;
        float total_leak = 0f;
        //if there's a leak in the pipe, some goes out the leak
        if (this.m_data.m_leak_severity != LeakSeverity.NONE)
        {
            float flat_leak = Mathf.Min(GetFlatLeakRatePerS() * dt, amountL);
            float remaining_after_flat = amountL - flat_leak;

            float capacity_leak = remaining_after_flat * GetCapacityLeakFraction();
            distributable = remaining_after_flat - capacity_leak;

            total_leak = flat_leak + capacity_leak;
        
            if (total_leak > 0f) 
            {
                LeakFluid(total_leak, dt, type);
            }
        }
        else
        { //no leak, all of it goes to downstream pipes
            distributable = amountL;
        }
        

        float total_passed = DistributeToDownstream(distributable, dt, type);
        
        // We report back what we successfully handled (leaked + passed)
        return total_leak + total_passed;
    }

    private float DistributeToDownstream(float amountL, float dt, FluidType type)
    {
        if( amountL <= 0f) return 0f;

        if (m_downstreams.Count == 0)
        {
            this.m_data.m_leak_severity = LeakSeverity.BROKEN;
        }
        m_cached_capacities.Clear();
        float total_capacity = 0f;

        for (int i = 0; i < m_downstreams.Count; i++)
        {
            float cap = m_downstreams[i].GetRemainingCapacityLitersThisDT(dt, type);
            m_cached_capacities.Add(cap);
            total_capacity += cap;
        }

        if (total_capacity <= 0f) return 0f;

        float total_received = 0f;
        for (int i = 0; i < m_downstreams.Count; i++)
        {
            float share = m_cached_capacities[i] / total_capacity;
            total_received += m_downstreams[i].ReceiveFluid(amountL * share, dt, type);
        }
        
        return total_received;
    }

    private void LeakFluid(float amountL, float dt, FluidType type)
    {
        TopicLogger.Log(LogTopic.FluidSystem, LogLevel.INFO,
            $"{name} leaked {amountL}L (severity: {m_data.m_leak_severity}).");

        if (m_downstream_leak_target != null)
        {
            m_downstream_leak_target.ReceiveFluid(amountL, dt, type);
        }
    }

    public float SendFluid(float amount_to_send_L, float dt, FluidType type)
    {
        return DistributeToDownstream(amount_to_send_L, dt, type);
    }

    void IFluidSender.AddDownstream(IFluidReceiver target)
    {
        AddDownstream(target);
    }

    public void SetDownstreamLeakTarget(IFluidReceiver target)
    {
        m_downstream_leak_target = target;
    }

    public void RemoveDownstreamLeakTarget(IFluidReceiver target)
    {
        if (m_downstream_leak_target != target)
        {
            Debug.LogWarning("Downstream leak target state broken. Trying to remove a target that isn't set.");
            return;
        }
        m_downstream_leak_target = null;
    }
}