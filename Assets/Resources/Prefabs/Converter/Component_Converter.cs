using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Component_PrefabBoundary))]

// Converter. Takes in fluids and converts them to other fluids at a rate defined in its Data_Converter. 
// Buffers incoming fluid in storage (capped at 2x its per-second conversion rate).
// Only converts when ALL inputs have at least rate_per_s * dt stored this tick.
// Wont accept fluid if the output port its attached to cant accept the conversion output
// If no output port, it will leak what it makes into the atmosphere
// Only ticks every UPDATE_TIC_s seconds, using the accumulated dt for conversion math
public class Component_Converter : MonoBehaviour, IFluidReceiver
{
    [SerializeField] private Data_Converter m_data;
    [SerializeField] private List<Component_FluidRelay> m_output_ports;
    [SerializeField] private List<Component_FluidRelay> m_input_ports;
    [SerializeField] private ParticleSystem m_particle_system;
    // Cached arrays for zero-allocation iteration in Update
    private FluidType[] m_inputFluidTypes;
    private FluidType[] m_outputFluidTypes;
    private const float UPDATE_TIC_s = .25f; //TODO all fluid processors should hae the same value here.
    private float update_tic_counter_s = 0f;
    private bool m_is_leaking = false;

    private void Awake()
    {
        m_particle_system = GetComponent<ParticleSystem>(); 
        // 1. Initialize component references
        if (m_output_ports == null)
        {
            m_output_ports = new List<Component_FluidRelay>(GetComponentsInChildren<Component_FluidRelay>(true));

        }

        if (m_input_ports == null)
        {
            m_input_ports = new List<Component_FluidRelay>(GetComponentsInChildren<Component_FluidRelay>(true));
        }
        for (int i = 0; i < m_input_ports.Count; i++)
        {
            m_input_ports[i].AddDownstream(this);
        }

        // 2. Initialize runtime dictionaries from Inspector data
        m_data.InitializeRuntimeData();

        // 3. Cache keys to prevent Garbage Collection allocations in Update()
        m_inputFluidTypes = new FluidType[m_data.input_conversion_rate_per_s.Count];
        m_data.input_conversion_rate_per_s.Keys.CopyTo(m_inputFluidTypes, 0);

        m_outputFluidTypes = new FluidType[m_data.output_conversion_rate_per_s.Count];
        m_data.output_conversion_rate_per_s.Keys.CopyTo(m_outputFluidTypes, 0);
    }

    private void Update()
    {
        update_tic_counter_s += Time.deltaTime;
        if (m_is_leaking)
        {
            if (!m_particle_system.isPlaying)
            {
                m_particle_system.Play();
            }
        }
        else
        {
            m_particle_system.Stop();
        }
        if (update_tic_counter_s < UPDATE_TIC_s)
        {
            return;
        }
        m_is_leaking = false;
        float dt = update_tic_counter_s; // actual elapsed time this tick, not the fixed constant

        if (CanConvert(dt))
        {
            ConvertAndOutput(dt);
        }

        update_tic_counter_s = 0f;
    }

    // All reagents must have at least rate_per_s * dt stored, or we don't convert at all this tick.
    private bool CanConvert(float dt)
    {
        for (int i = 0; i < m_inputFluidTypes.Length; i++)
        {
            FluidType inputType = m_inputFluidTypes[i];
            float required = m_data.input_conversion_rate_per_s[inputType] * dt;

            if (!m_data.storage_liters.TryGetValue(inputType, out float stored) || stored < required)
            {
                //Debug.Log($"NOT Converting: {inputType} has {stored} stored, needs {required}");
                return false;
            }
        }
        return true;
    }

    private void ConvertAndOutput(float dt)
    {
        Debug.Log($"Converting with dt {dt}");

        // 1. Consume stored inputs
        for (int i = 0; i < m_inputFluidTypes.Length; i++)
        {
            FluidType inputType = m_inputFluidTypes[i];
            float amountConsumed = m_data.input_conversion_rate_per_s[inputType] * dt;
            m_data.storage_liters[inputType] -= amountConsumed;
        }

        // 2. Push produced outputs downstream
        for (int i = 0; i < m_outputFluidTypes.Length; i++)
        {
            FluidType outputType = m_outputFluidTypes[i];
            float amountToOutput = m_data.output_conversion_rate_per_s[outputType] * dt;
            float remainingToPush = amountToOutput;

            if (m_output_ports.Count < 1)
            {
                Debug.Log($"Leaked {amountToOutput} liters {outputType}");
                m_is_leaking = true;
            }
            else
            {
                for (int p = 0; p < m_output_ports.Count; p++)
                {
                    if (remainingToPush <= 0f) break;
                    float pushed = m_output_ports[p].ReceiveFluid(remainingToPush, dt, outputType);
                    remainingToPush -= pushed;
                   
                }
            }
            
        }
    }

    public float GetRemainingCapacityLitersThisDT(float dt, FluidType fluid)
    {
        if (!m_data.storage_liters.TryGetValue(fluid, out float stored))
            return 0f;

        float maxStorage = m_data.max_storage_liters[fluid];
        float space = maxStorage - stored;

        if (space <= 0f)
            return 0f;

        // PREDICTIVE BACKPRESSURE: Check if downstreams have space BEFORE accepting
        if (!CanDownstreamsAcceptOutputs(dt))
            return 0f;

        return space;
    }

    public float ReceiveFluid(float amountL, float dt, FluidType type)
    {
        if (!m_data.storage_liters.TryGetValue(type, out float stored))
            return 0f;

        float maxStorage = m_data.max_storage_liters[type];
        float space = maxStorage - stored;
        float accepted = Mathf.Min(amountL, space);

        if (accepted <= 0f)
            return 0f;

        m_data.storage_liters[type] = stored + accepted;
        return accepted;
    }

    private bool CanDownstreamsAcceptOutputs(float dt)
    {
        // Check every output fluid we generate
        if (m_output_ports.Count < 1)
        {
            return true;
        }
        for (int i = 0; i < m_outputFluidTypes.Length; i++)
        {
            FluidType outputType = m_outputFluidTypes[i];
            float requiredCapacity = m_data.output_conversion_rate_per_s[outputType] * dt;
            float availableCapacity = 0f;

            // Sum up the capacity of all connected output ports
            for (int p = 0; p < m_output_ports.Count; p++)
            {
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
    public Dictionary<FluidType, float> storage_liters { get; private set; }
    public Dictionary<FluidType, float> max_storage_liters { get; private set; }

    public void InitializeRuntimeData()
    {
        input_conversion_rate_per_s = new Dictionary<FluidType, float>();
        output_conversion_rate_per_s = new Dictionary<FluidType, float>();
        storage_liters = new Dictionary<FluidType, float>();
        max_storage_liters = new Dictionary<FluidType, float>();

        foreach (var input in inspector_inputs)
        {
            input_conversion_rate_per_s[input.fluid] = input.conversation_rate_liters_per_second;
            storage_liters[input.fluid] = 0f;
            max_storage_liters[input.fluid] = input.conversation_rate_liters_per_second * 2f;
        }

        foreach (var output in inspector_outputs)
        {
            output_conversion_rate_per_s[output.fluid] = output.conversation_rate_liters_per_second;
        }
    }
}

// Struct to allow editing rates in the Unity Inspector
[System.Serializable]
public struct FluidConversion
{
    public FluidType fluid;
    public float conversation_rate_liters_per_second;
}