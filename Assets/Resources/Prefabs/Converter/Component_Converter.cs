using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Component_PrefabBoundary))]

// Converter. Takes in fluids and converts them to other fluids at a rate defined in its Data_Converter. 
// Will only accept fluid per second in an amount it can convert per second. 
// Wont accept fluid it the output port its attached to cant accept the conversion output
// If no output port, it will leak what it makes into the atmosphere
// Only runs every UPDATE_TIC_s seconds. So it stores its capacity_per_second * UPDATE_TIC_s and then converts it all
public class Component_Converter : MonoBehaviour, IFluidReceiver
{
    [SerializeField] private Data_Converter m_data;
    [SerializeField] private List<Component_FluidRelay> m_output_ports;
    [SerializeField] private List<Component_FluidRelay> m_input_ports;
    
    // Cached arrays for zero-allocation iteration in Update
    private FluidType[] m_inputFluidTypes;
    private FluidType[] m_outputFluidTypes;
    private const float UPDATE_TIC_s = .25f; //TODO all fluid processors should hae the same value here.
    private float update_tic_counter_s = 0f;

    private void Awake()
    {
        // 1. Initialize component references
        m_output_ports = new List<Component_FluidRelay>(GetComponentsInChildren<Component_FluidRelay>(true));
        
        m_input_ports = new List<Component_FluidRelay>(GetComponentsInChildren<Component_FluidRelay>(true));
        for (int i = 0; i < m_input_ports.Count; i++)
        {
            m_input_ports[i].AddDownstream(this);
        }

        // 2. Initialize runtime dictionaries from Inspector data
        m_data.InitializeRuntimeData(UPDATE_TIC_s);

        // 3. Cache keys to prevent Garbage Collection allocations in Update()
        m_inputFluidTypes = new FluidType[m_data.input_conversion_rate_per_s.Count];
        m_data.input_conversion_rate_per_s.Keys.CopyTo(m_inputFluidTypes, 0);

        m_outputFluidTypes = new FluidType[m_data.output_conversion_rate_per_s.Count];
        m_data.output_conversion_rate_per_s.Keys.CopyTo(m_outputFluidTypes, 0);
    }

    private void Update()
    {
        update_tic_counter_s += Time.deltaTime;
        if (update_tic_counter_s < UPDATE_TIC_s)
        {
            return;
        }

        // 1. Process conversions for any fluid that had capacity consumed this tick
        for (int i = 0; i < m_inputFluidTypes.Length; i++)
        {
            FluidType inputType = m_inputFluidTypes[i];
            float fullCapacity = m_data.input_conversion_rate_per_s[inputType] * UPDATE_TIC_s;

            if (m_data.remaining_capacity_this_tick.TryGetValue(inputType, out float remaining) && remaining < fullCapacity)
            {
                TriggerConversionAndOutput(inputType, UPDATE_TIC_s); // fixed tick dt, not the drifting accumulator
            }
        }

        // 2. Refill capacity for the next tick
        for (int i = 0; i < m_inputFluidTypes.Length; i++)
        {
            FluidType inputType = m_inputFluidTypes[i];
            m_data.remaining_capacity_this_tick[inputType] = m_data.input_conversion_rate_per_s[inputType] * UPDATE_TIC_s;
        }
        update_tic_counter_s = 0f;
    }

    private void TriggerConversionAndOutput(FluidType inputType, float dt)
    {
        for (int i = 0; i < m_outputFluidTypes.Length; i++)
        {
            FluidType outputType = m_outputFluidTypes[i];
            float amountToOutput = m_data.output_conversion_rate_per_s[outputType] * dt;
            float remainingToPush = amountToOutput;

            for (int p = 0; p < m_output_ports.Count; p++)
            {
                if (remainingToPush <= 0f) break;
                float pushed = m_output_ports[p].ReceiveFluid(remainingToPush, dt, outputType);
                remainingToPush -= pushed;
            }
        }
    }

    public float GetRemainingCapacityLitersThisDT(float dt, FluidType fluid)
    {
        if (!m_data.remaining_capacity_this_tick.TryGetValue(fluid, out float remaining))
            return 0f;

        if (remaining <= 0f)
            return 0f;

        // PREDICTIVE BACKPRESSURE: Check if downstreams have space BEFORE accepting
        if (!CanDownstreamsAcceptOutputs(dt))
            return 0f;

        return remaining;
    }

    public float ReceiveFluid(float amountL, float dt, FluidType type)
    {
        if (!m_data.remaining_capacity_this_tick.TryGetValue(type, out float remaining))
            return 0f;

        float accepted = Mathf.Min(amountL, remaining);
        m_data.remaining_capacity_this_tick[type] = remaining - accepted;

        return accepted;
    }

    private bool CanDownstreamsAcceptOutputs(float dt)
    {
        // Check every output fluid we generate
        for (int i = 0; i < m_outputFluidTypes.Length; i++)
        {
            FluidType outputType = m_outputFluidTypes[i];
            float requiredCapacity = m_data.output_conversion_rate_per_s[outputType] * dt;
            float availableCapacity = 0f;

            // Sum up the capacity of all connected output ports
            for (int p = 0; p < m_output_ports.Count; p++)
            {
                // Assuming Component_FluidRelay implements IFluidReceiver
                availableCapacity += m_output_ports[p].GetRemainingCapacityLitersThisDT(dt, outputType);
            }

            // If any output fluid doesn't have enough space, we stall the whole machine
            if (availableCapacity < requiredCapacity)
            {
                return false; 
            }
        }
        
        return true; 
    }
}

[System.Serializable]
public class Data_Converter
{
    // Unity does NOT serialize Dictionaries. We use lists of structs for the Inspector.
    [SerializeField] private List<FluidConversion> inspector_inputs = new List<FluidConversion>();
    [SerializeField] private List<FluidConversion> inspector_outputs = new List<FluidConversion>();

    public Dictionary<FluidType, float> input_conversion_rate_per_s { get; private set; }
    public Dictionary<FluidType, float> output_conversion_rate_per_s { get; private set; }
    public Dictionary<FluidType, float> remaining_capacity_this_tick { get; private set; } 

    public void InitializeRuntimeData(float tickDuration_s)
    {
        input_conversion_rate_per_s = new Dictionary<FluidType, float>();
        output_conversion_rate_per_s = new Dictionary<FluidType, float>();
        remaining_capacity_this_tick = new Dictionary<FluidType, float>();

        foreach (var input in inspector_inputs)
        {
            input_conversion_rate_per_s[input.fluid] = input.rate;
            remaining_capacity_this_tick[input.fluid] = input.rate * tickDuration_s; 
        }

        foreach (var output in inspector_outputs)
        {
            output_conversion_rate_per_s[output.fluid] = output.rate;
        }
    }
}

// Struct to allow editing rates in the Unity Inspector
[System.Serializable]
public struct FluidConversion
{
    public FluidType fluid;
    public float rate;
}