using UnityEngine;

public class Component_Lightbulb : MonoBehaviour, IPowerConsumer, IToggleable, IPowerNetworked
{
    [SerializeField, Required] private GameObject m_power_slot;
    [SerializeField] private Component_PowerNode m_connected_power_node;
    [SerializeField] private float m_power_radius;
    [SerializeField] private float m_power_usage_per_second;
    [SerializeField] private bool m_wants_to_be_on;
    [SerializeField, Required] private GameObject m_light_toggle;
    private bool m_needs_startup_register = true;

    private void Start()
    {
        RefreshLightVisual();
    }

    private void Update()
    {
        if (m_needs_startup_register)
        {
            m_needs_startup_register = TryGameStartNetworkConnect();
        }
    }

    //single source of truth: re-derive power status on demand and set
    //the light active only if we're both powered and want to be on.
    //called on toggle, and whenever the network notifies us that our
    //power situation may have changed (so we don't have to poll every frame).
    private void RefreshLightVisual()
    {
        m_light_toggle.SetActive(m_wants_to_be_on && CanToggle(out _));
    }

    public bool CanToggle(out string reason)
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

    public void Toggle()
    {
        AudioEvents.Fire(SoundID.Toggle, transform.position);
        m_wants_to_be_on = !m_wants_to_be_on;
        RefreshLightVisual();
    }

    public bool IsOn()
    {
        return m_wants_to_be_on;
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
        if (m_light_toggle.activeSelf == false)
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
        RefreshLightVisual();
    }

    public void SetHasPower()
    {
        RefreshLightVisual();
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