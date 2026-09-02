using System.Collections.Generic;
using UnityEngine;



public class HeatManager : MonoBehaviour
{
    public static HeatManager Instance { get; private set; }

    private static readonly List<Component_Temperature> s_pending = new List<Component_Temperature>();

    private readonly HashSet<Component_Temperature> m_registered = new HashSet<Component_Temperature>();
    private readonly List<Component_Temperature> m_tick_buffer = new List<Component_Temperature>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;//singleton so only one heat manager can exist
        }
        Instance = this;

        foreach (Component_Temperature temp in s_pending)
        {
            if (temp != null)
            {
                m_registered.Add(temp);
            }
        }
           
        s_pending.Clear();
    }

    public static void Register(Component_Temperature temp)
    {
        if (Instance != null) Instance.m_registered.Add(temp);
        else s_pending.Add(temp);
    }

    public static void Unregister(Component_Temperature temp)
    {
        if (Instance != null) Instance.m_registered.Remove(temp);
        else s_pending.Remove(temp);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        m_tick_buffer.Clear();
        m_tick_buffer.AddRange(m_registered);

        foreach (Component_Temperature temp in m_tick_buffer)
        {
            if (!temp)
            {
                m_registered.Remove(temp); 
                continue;
            }
            if (!temp.IsMelting)
            {
                continue;
            }

            var health = temp.CachedHealth;
            if (health == null || health.IsDestroyed)
            {
                continue;
            }

            health.TakeDamage(temp.MeltDamagePerSecond * dt);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}