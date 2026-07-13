using UnityEngine;

[System.Serializable]
public class Data_FluidReceiver
{
    public bool m_active = true;
    public GameObject m_prefab;
    public float receive_rate_L_s; //liters per second
}
[RequireComponent(typeof(Component_PrefabBoundary))]
public class Component_FluidReceiver : MonoBehaviour
{
    // The tank this receiver is attached to
    [SerializeField] private Component_FluidTank m_parentTank;
    [SerializeField] private Data_FluidReceiver m_data = new Data_FluidReceiver();

    internal void SetSource(Component_FluidTank component_FluidTank)
    {
        m_parentTank = component_FluidTank;
    }

    internal float GetRemainingCapacityLitersThisDT(float seconds)
    {
        if (m_parentTank == null || !m_data.m_active)
        {
            return 0f;
        }

        float parent_tank_capacity = m_parentTank.GetRemainingCapacity();
        // Returns either the space left or the maximum the port can handle, whichever is smaller
        return Mathf.Min(parent_tank_capacity, m_data.receive_rate_L_s * seconds);

    }

    internal float ReceiveFluid(float amount_L)
    {
        if (m_parentTank == null) return 0f;

        // Tell the parent tank to add the fluid and return how much it accepted
        return m_parentTank.AddFluid(amount_L);
    }
}