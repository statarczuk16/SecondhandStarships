using System.Collections.Generic;
using UnityEngine;

public enum LeakSeverity
{
    NONE,
    LEAK,
    BROKEN
}

[RequireComponent(typeof(Component_PrefabBoundary))]
public class Component_FluidRelay : MonoBehaviour, IFluidReceiver, IFluidSender
{
    [SerializeField] private List<IFluidReceiver> m_downstreams; // each must implement IFluidReceiver
    [SerializeField] private IFluidReceiver m_downstream_leak_target; // each must implement IFluidReceiver

    [SerializeField] private LeakSeverity m_leak_severity = LeakSeverity.NONE;
    [SerializeField] private float m_leak_flat_L_s = 0.5f;
    [SerializeField] private float m_broken_flat_L_s = 3f;

    private const float LEAK_CAPACITY_FRACTION = 0.5f;
    private const float BROKEN_CAPACITY_FRACTION = 1f;

    private void Awake()
    {
        m_downstreams = new List<IFluidReceiver>();
    }

    internal void AddDownstream(IFluidReceiver source)
    {
        m_downstreams.Add(source);
    }

    private float GetFlatLeakRate() => m_leak_severity switch
    {
        LeakSeverity.LEAK => m_leak_flat_L_s,
        LeakSeverity.BROKEN => m_broken_flat_L_s,
        _ => 0f
    };

    private float GetCapacityLeakFraction() => m_leak_severity switch
    {
        LeakSeverity.LEAK => LEAK_CAPACITY_FRACTION,
        LeakSeverity.BROKEN => BROKEN_CAPACITY_FRACTION,
        _ => 0f
    };

    public float GetRemainingCapacityLitersThisDT(float dt)
    {
        float downstream_total = 0f;
        foreach (var d in m_downstreams)
        {
            downstream_total += d.GetRemainingCapacityLitersThisDT(dt);
        }
        return downstream_total + (GetFlatLeakRate() * dt);
    }

    public float ReceiveFluid(float amountL, float dt)
    {
        TopicLogger.Log(LogTopic.FluidSystem, LogLevel.INFO, $"{this.name} got fluid!");
        float flat_leak = Mathf.Min(GetFlatLeakRate() * dt, amountL);
        float remaining_after_flat = amountL - flat_leak;

        float capacity_leak = remaining_after_flat * GetCapacityLeakFraction();
        float distributable = remaining_after_flat - capacity_leak;

        float total_leak = flat_leak + capacity_leak;
        if (total_leak > 0f) LeakFluid(total_leak);

        float total_passed = DistributeToDownstream(distributable, dt);
        return total_leak + total_passed;
    }

    private float DistributeToDownstream(float amountL, float dt)
    {
        if (m_downstreams.Count == 0 || amountL <= 0f) return 0f;

        var capacities = new float[m_downstreams.Count];
        float total_capacity = 0f;
        for (int i = 0; i < m_downstreams.Count; i++)
        {
            capacities[i] = m_downstreams[i].GetRemainingCapacityLitersThisDT(dt);
            total_capacity += capacities[i];
        }

        if (total_capacity <= 0f) return 0f;

        float total_received = 0f;
        for (int i = 0; i < m_downstreams.Count; i++)
        {
            
            float share = capacities[i] / total_capacity;
            total_received += m_downstreams[i].ReceiveFluid(amountL * share, dt);
        }
        return total_received;
    }

    private void LeakFluid(float amountL)
    {
        TopicLogger.Log(LogTopic.FluidSystem, LogLevel.INFO, $"{name} leaked {amountL}L (severity: {m_leak_severity}).");
    }

    public void SendFluid(float amount_to_send_L, float dt)
    {
        DistributeToDownstream(amount_to_send_L, dt);
    }

    void IFluidSender.AddDownstream(IFluidReceiver target)
    {
        AddDownstream(target);
    }

    public void SetDownstreamLeakTarget(IFluidReceiver target)
    {
        m_downstreams.Remove(m_downstream_leak_target);
        m_downstream_leak_target = target;
        m_downstreams.Add(target);
    }

    public void RemoveDownstreamLeakTarget(IFluidReceiver target)
    {
        if(m_downstream_leak_target != target)
        {
            Debug.Log("Downstream leak target state broken");
            return;
        }
        m_downstreams.Remove(target);
        m_downstream_leak_target = null;
    }
}