using System;
using UnityEngine;

public class Component_Lightbulb : MonoBehaviour, IToggleable
{

    [SerializeField] private bool m_wants_to_be_on;
    [SerializeField] private bool m_is_on;
    [SerializeField, Required] private GameObject m_light_toggle;
    [SerializeField, Required] private Component_PowerConsumer m_power_consumer;
    [SerializeField, Required] private bool m_has_power = false;
    private Action m_onLostPower;
    private Action m_onGainedPower;
    
    private void Start()
    {
        RefreshActualOnState();
    }
    
  

    private void OnEnable()
    {
        if (m_power_consumer == null)
            return;

       
        m_onLostPower = () =>
        {
            m_has_power = false;
            RefreshActualOnState();
        };

        m_onGainedPower = () =>
        {
            m_has_power = true;
            RefreshActualOnState();
        };

        m_power_consumer.OnLostPower += m_onLostPower;
        m_power_consumer.OnGainedPower += m_onGainedPower;
        m_power_consumer.SetPassiveConsumptionOn(m_wants_to_be_on);
    }

    private void OnDisable()
    {
        if (m_power_consumer == null)
            return;

        m_power_consumer.OnLostPower -= m_onLostPower;
        m_power_consumer.OnGainedPower -= m_onGainedPower;
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
        m_power_consumer.SetPassiveConsumptionOn(m_wants_to_be_on);
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
    
    private void RefreshActualOnState()
    {
        bool should_be_on = m_wants_to_be_on && OnRequirementsMet(out _);
        if (should_be_on) TurnOn();
        else TurnOff();
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

        if (!m_has_power)
        {
            reason = "//ERROR: NO POWER TO LIGHTS";
            return false;
        }
        return true;
    }


}