using UnityEngine;

[System.Serializable]
public class Data_FluidReceiver
{
    public bool m_active = true;
    public GameObject m_prefab;
    public float receive_rate_L_s;
}

[RequireComponent(typeof(Component_PrefabBoundary))]
public class Component_TankInputPort : MonoBehaviour, IFluidReceiver
{
    [SerializeField] private IFluidReceiver m_parentTank;
    [SerializeField] private Data_FluidReceiver m_data = new Data_FluidReceiver();

    internal void SetDownstream(Component_FluidTank component_FluidTank)
    {
        m_parentTank = component_FluidTank;
    }

    public float GetRemainingCapacityLitersThisDT(float dt)
    {
        if (m_parentTank == null || !m_data.m_active) return 0f;
        float parent_tank_capacity = m_parentTank.GetRemainingCapacityLitersThisDT(dt);
        return Mathf.Min(parent_tank_capacity, m_data.receive_rate_L_s * dt);
    }

    public float ReceiveFluid(float amountL, float dt)
    {
        if (m_parentTank == null) return 0f;
        return m_parentTank.ReceiveFluid(amountL, dt);
    }
}