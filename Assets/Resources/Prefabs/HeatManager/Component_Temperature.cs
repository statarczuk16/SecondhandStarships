using UnityEngine;

[System.Serializable]
public class Data_Temperature
{
    public float m_current_temp_c;
    public int m_melting_point_c;
    public int m_freezing_point_c;
    public float m_melt_damage_per_second = 5f;
}

public class Component_Temperature : MonoBehaviour
{
    [SerializeField] private Data_Temperature m_data;

    private Component_Health m_cached_health;

    public bool IsMelting => m_data.m_current_temp_c >= m_data.m_melting_point_c;
    public bool IsFreezing => m_data.m_current_temp_c <= m_data.m_freezing_point_c;
    public float MeltDamagePerSecond => m_data.m_melt_damage_per_second;
    public Component_Health CachedHealth => m_cached_health;

    public int GetMeltingPointC() => m_data.m_melting_point_c;
    public int GetFreezingPointC() => m_data.m_freezing_point_c;

    private void OnEnable()
    {
        TryGetComponent(out m_cached_health);
        HeatManager.Register(this);
    }

    private void OnDisable()
    {
        HeatManager.Unregister(this);
    }

    public void AddHeat(float amount)
    {
        if (amount <= 0f) return;
        m_data.m_current_temp_c += amount;
    }

    public void SubtractHeat(float amount)
    {
        if (amount <= 0f) return;
        m_data.m_current_temp_c -= amount;
    }

    public float GetHeat() => m_data.m_current_temp_c;
}