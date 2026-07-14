using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]


public enum FluidType
{
    WATER,
    GAS,
    OXYGEN,
}
[RequireComponent(typeof(Component_PrefabBoundary))]
public class Component_FluidTank : MonoBehaviour, IFluidReceiver
{
    [SerializeField] private Data_FluidTank m_data;
    [SerializeField] private List<Component_TankOutputPort> m_output_ports;//they send FOR me
    [SerializeField] private List<Component_FluidRelay> m_input_ports;//they receive FOR me 
    [SerializeField] private Component_FluidFillShader m_fluid_shader;

    private void Awake()
    {
        m_output_ports = new List<Component_TankOutputPort>(GetComponentsInChildren<Component_TankOutputPort>(true));
        foreach (Component_TankOutputPort port in m_output_ports)
        {
            port.SetSource(this);
        }
        m_input_ports = new List<Component_FluidRelay>(GetComponentsInChildren<Component_FluidRelay>(true));
        foreach (Component_FluidRelay port in m_input_ports)
        {
            port.AddDownstream(this);
        }
        m_fluid_shader = GetComponentInChildren<Component_FluidFillShader>();
    }

    private void Update()
    {
        if (m_fluid_shader)
        {
            m_fluid_shader.SetFillPercent(this.m_data.m_current_L / this.m_data.m_max_L);
        }
    }


    internal float AddFluid(float amount_L)
    {
        if (amount_L <= 0) return 0f;

        float spaceAvailable = m_data.m_max_L - m_data.m_current_L;
        float amountToAdd = Mathf.Min(amount_L, spaceAvailable);

        m_data.m_current_L += amountToAdd;

        if (m_data.m_current_L >= m_data.m_max_L)
        {
            OnCapacityReached();
        }
        
        return amountToAdd;
    }

    internal float TakeFluid(float amount_L)
    {
        if (amount_L <= 0) return 0f;

        float amountToTake = Mathf.Min(amount_L, m_data.m_current_L);

        m_data.m_current_L -= amountToTake;

        if (m_data.m_current_L <= 0f)
        {
            OnEmpty();
        }
        
        return amountToTake;
    }

    private void OnCapacityReached()
    {
        Debug.Log($"{gameObject.name} is full.");
        // Add custom logic for when the tank is full
    }

    private void OnEmpty()
    {
        Debug.Log($"{gameObject.name} is empty.");
        // Add custom logic for when the tank is empty
    }


    internal float GetCurrentFluidAmount()
    {
        return this.m_data.m_current_L;
    }

    public float GetRemainingCapacityLitersThisDT(float dt)
    {
        return Mathf.Clamp(this.m_data.m_max_L - this.m_data.m_current_L, 0f, this.m_data.m_max_L);
    }

    public float ReceiveFluid(float amountL, float dt)
    {
        return AddFluid(amountL);
    }


}

[System.Serializable]
public class Data_FluidTank
{
     public float m_max_L;
     public float m_current_L;
     public FluidType m_fluid_type;    
}