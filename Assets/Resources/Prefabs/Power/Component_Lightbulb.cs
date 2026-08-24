using UnityEngine;

public class Component_Lightbulb : MonoBehaviour, IPowerConsumer, IToggleable, IPowerNetworked
{
    [SerializeField, Required] private GameObject m_power_slot;
    [SerializeField] private Component_PowerNode m_connected_power_node;
    [SerializeField] private float m_power_radius;
    [SerializeField] private float m_power_usage_per_second;
    [SerializeField] private bool m_wants_to_be_on;
    [SerializeField] private bool m_is_on;
    [SerializeField, Required] private GameObject m_light_toggle;
    private bool m_needs_startup_register = true;

    private void Start()
    {
        RefreshActualOnState();
    }
    
    private void RefreshActualOnState()
    {
        bool should_be_on = m_wants_to_be_on && OnRequirementsMet(out _);
        if (should_be_on == m_is_on)
        {
            return;
        }
        if (should_be_on) TurnOn();
        else TurnOff();
    }

    private void Update()
    {
        if (m_needs_startup_register)
        {
            m_needs_startup_register = TryGameStartNetworkConnect();
        }
    }
    
    public bool CanToggle(out string reason)
    {
        reason = "";
        return true;
    }

    public void ToggleWantsToBeOn()
    {
        AudioEvents.Fire(SoundID.Toggle, transform.position);
        m_wants_to_be_on = !m_wants_to_be_on;
        RefreshActualOnState();
    }

    public bool IsOn()
    {
        return m_is_on;
    }

    public bool WantsToBeOn()
    {
        return m_wants_to_be_on;
    }

    public void TurnOff()
    {
        m_is_on = false;
        m_light_toggle.SetActive(false);
    }

    public void TurnOn()
    {
        m_is_on = true;
        m_light_toggle.SetActive(true);
    }

    public bool OnRequirementsMet(out string reason)
    {
       reason = "Whatever";

        if (!CheckHasPower())
        {
            reason = "//ERROR: NO POWER TO LIGHTS";
            return false;
        }

        if (!this.m_connected_power_node.CanDrawPower(m_power_usage_per_second))
        {
            float available = this.m_connected_power_node.GetOwningNetwork().GetAvailablePower();
            reason = $"//ERROR: POWER NETWORK INSUFFICIENT (NEED {m_power_usage_per_second} UNIT/S) BUT (NETWORK HAS: {available} UNITS)";
            return false;
        }

        return true;
    }
    
    public Component_PowerNode TryFindPowerNode()
    {
        var colliders = Physics.OverlapSphere(this.m_power_slot.transform.position, this.GetPowerRadius_M(), Physics.AllLayers, QueryTriggerInteraction.Ignore);
        foreach (var collider in colliders)
        {
            if (collider.GetComponentInParent<Component_PowerNode>() != null)
            {
                Component_PowerNode component_PowerNode = collider.GetComponentInParent<Component_PowerNode>();
                if (component_PowerNode.GetOwningNetwork() && component_PowerNode.GetOwningNetwork().CheckHasPower())
                {
                    return component_PowerNode;
                }
            }
        }
        return null;
    }

    public void ConnectToNode(Component_PowerNode node)
    {
        if (m_connected_power_node != null)
        {
            DisconnectFromNode();
        }
        m_connected_power_node = node;
        node.ConnectConsumer(this);
    }

    public void DisconnectFromNode()
    {
        m_connected_power_node.DisconnectConsumer(this);
        m_connected_power_node = null;
    }

    public float PowerConsumedPerDT(float dt)
    {
        if (!IsOn())
        {
            return 0f;
        }
        return this.m_power_usage_per_second * dt;
    }

    public float PowerNeededPerUsage()
    {
        return 0;
    }

    public bool CheckHasPower()
    {
        if (this.m_connected_power_node == null)
        {
            var node = this.TryFindPowerNode();
            if (node)
            {
                ConnectToNode(node);
            }
            else
            {
                return false;
            }
        }

        if (this.m_connected_power_node == null)
        {
            return false;
        }

        return this.m_connected_power_node.GetOwningNetwork().CheckHasPower();
    }

    //network telling us our power status may have changed - go check and refresh
    public void SetPoweredStarved()
    {
        RefreshActualOnState();
    }

    public void SetHasPower()
    {
        RefreshActualOnState();
    }

    public float GetPowerRadius_M()
    {
        return m_power_radius;
    }

    public bool TryGameStartNetworkConnect()
    {
        if (m_connected_power_node == null)
        {
            m_connected_power_node = TryFindPowerNode();
        }

        if (m_connected_power_node == null)
        {
            return true;
        }
        if (m_connected_power_node.GetOwningNetwork() != null)
        {
            if (m_connected_power_node.GetOwningNetwork() != null)
            {
                ConnectToNode(m_connected_power_node);
                TopicLogger.Log(LogTopic.PowerSystem, LogLevel.INFO, $"First time connect! {this.name}");
                return false;
            }
        }
        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Color color = Color.yellow;
        color.a = 0.05f;
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, this.m_power_radius);
    }
#endif
}