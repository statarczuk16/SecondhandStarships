using System;
using UnityEngine;


public interface IDamageable
{
    bool IsDestroyed { get; }
    void TakeDamage(float amount);
}

[System.Serializable]
public class Data_Health
{
    public float m_current_hp;
    public float m_max_hp;
    public bool m_is_destroyed;
}

public class Component_Health : MonoBehaviour, IDamageable
{
    [SerializeField] private Data_Health m_data;

    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDestroyed;

    public bool IsDestroyed => m_data.m_is_destroyed;
    public float CurrentHp => m_data.m_current_hp;
    public float MaxHp => m_data.m_max_hp;

    private void Awake()
    {
        if (m_data.m_current_hp <= 0f && m_data.m_max_hp > 0f)
        {
            m_data.m_current_hp = m_data.m_max_hp;
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDestroyed || amount <= 0f) return;

        m_data.m_current_hp = Mathf.Max(0f, m_data.m_current_hp - amount);
        OnHealthChanged?.Invoke(m_data.m_current_hp, m_data.m_max_hp);

        if (m_data.m_current_hp <= 0f)
        {
            m_data.m_is_destroyed = true;
            TopicLogger.Log(LogTopic.General, LogLevel.INFO, $"{this.name} destroyed");
            OnDestroyed?.Invoke();
        }
    }
    
    public void HealDamage(float amount)
    {
        if (IsDestroyed || amount <= 0f) return;

        m_data.m_current_hp = Mathf.Max(0f, m_data.m_current_hp + amount);
        OnHealthChanged?.Invoke(m_data.m_current_hp, m_data.m_max_hp);

        if (m_data.m_current_hp <= 0f)
        {
            m_data.m_is_destroyed = true;
            TopicLogger.Log(LogTopic.General, LogLevel.INFO, $"{this.name} destroyed");
            OnDestroyed?.Invoke();
        }
    }
}