using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.InputSystem.XR;



[System.Serializable]
public class Data_FluidSender
{
    public bool m_active;

    public float send_rate_L_s = 1f;

    public float launchSpeed = 6f;

    public int sampleCount = 30;

    public float sampleStep = 0.05f;
}
[System.Serializable]
[RequireComponent(typeof(Component_PrefabBoundary))]
public class Component_TankOutputPort : MonoBehaviour, IInteractable, IHighlightable, IFluidSender
{

    [SerializeField] private Data_FluidSender m_data = new Data_FluidSender();
    public Component_FluidTank m_source;
    [SerializeField]  public IFluidReceiver m_piped_receiver; //pipe connection. stops leaks
    public IFluidReceiver m_drain_pour_receiver;//if we leak, may land in a bucket etc. 
    public IFluidReceiver m_active_receiver;
    private const float UPDATE_TIC_s = .25f;
    private float update_tic_counter_s = 0f;
    private float fluid_sent_last_update = 0f;

    [SerializeField] private FluidStreamVisual m_streamVisual;
    [SerializeField] private FluidStreamVisual m_streamVisualPrefab;

    [SerializeField] public Component m_debug_boostrap_receiver;//just exists so we can assign in editor
    private HighlightableRenderer m_highlight_renderer;


    public Transform InteractionPoint => this.transform;

    private void Awake()
    {
        m_highlight_renderer = this.GetComponent<HighlightableRenderer>();
        if(m_debug_boostrap_receiver)
        {
            m_piped_receiver = m_debug_boostrap_receiver.GetComponent<Component_FluidRelay>() as IFluidReceiver;
        }
    }

    public void SetSource(Component_FluidTank component_FluidTank)
    {
        m_source = component_FluidTank;
    }

    public void SetDownstreamLeakTarget(IFluidReceiver target)
    {
        m_drain_pour_receiver = target;
    }

    public void AddDownstream(IFluidReceiver target)
    {
        m_piped_receiver = target;
    }

    private static bool IsFluidReceiverAlive(IFluidReceiver receiver)
    {
        return (receiver as UnityEngine.Object) != null;
    }

    private void Update()
    {



        if (m_source == null || m_data.m_active == false)
        {
            StopStreamSimulation();
            return;
        }

        // If we dont have a pipe fitted and sent water last update, show 
        // graphics for water pouring out of the spout
        // even if the bucket is full (sent fluid will be 0)
        // we still need to show the leaking water graphics
        bool piped = IsFluidReceiverAlive(m_piped_receiver);
        bool drainPouring = IsFluidReceiverAlive(m_drain_pour_receiver);

        if (!piped && fluid_sent_last_update > 0f || drainPouring)
        {
            EnsureStreamVisual();
            UpdateStreamSimulation();
        }
        else
        {
            StopStreamSimulation();
            if (piped)
            {
                m_drain_pour_receiver = null;
            }
        }

        // send water to a pipe if we have one. else send to a bucket we are draining into.
        if (piped)
        {
            m_active_receiver = m_piped_receiver;
        }
        else if (drainPouring)
        {
            m_active_receiver = m_drain_pour_receiver;
        }
        else
        {
            m_active_receiver = null;
        }


        update_tic_counter_s += Time.deltaTime;
        if (update_tic_counter_s < UPDATE_TIC_s)
        {
            return;
        }
        fluid_sent_last_update = 0f;

        float amount_I_can_send = Mathf.Min(m_source.GetCurrentFluidAmount(), m_data.send_rate_L_s * update_tic_counter_s);

        float amount_they_can_receive = 0f;
        if (m_active_receiver == null)
        {
            amount_they_can_receive = amount_I_can_send; //no tank or connection so I just leak everywhere as fast as I can
        }
        else
        {
            amount_they_can_receive = m_active_receiver.GetRemainingCapacityLitersThisDT(update_tic_counter_s);
        }
        float final_transfer_amount = Mathf.Min(amount_I_can_send, amount_they_can_receive);
        this.fluid_sent_last_update = final_transfer_amount;
        SendFluid(final_transfer_amount, update_tic_counter_s);
        update_tic_counter_s = 0f;
    }

    private void SendFluid(float amount_to_send_L, float dt)
    {
        float amount_from_my_tank_L = this.m_source.TakeFluid(amount_to_send_L);

        if (m_active_receiver != null)
        {
            float amount_sent = m_active_receiver.ReceiveFluid(amount_from_my_tank_L, dt);
            if (amount_to_send_L != amount_sent)
            {
                TopicLogger.Log(LogTopic.FluidSystem, LogLevel.WARN, $"Thought we could send {amount_to_send_L} but could only send {amount_sent}?");
            }
        }
        else
        {
            //If we get here, we have no pipe connection neither is the stream spout hitting a bucket etc.
            TopicLogger.Log(LogTopic.FluidSystem, LogLevel.INFO, $"Fluid tank leaked {amount_from_my_tank_L}L with nothing to catch it.");
        }
    }


    private void EnsureStreamVisual()
    {
        if (m_streamVisual != null)
        {
            return;
        }

        if (m_streamVisualPrefab == null)
        {
            TopicLogger.Log(LogTopic.FluidSystem, LogLevel.WARN, $"{name} has no stream visual prefab assigned.");
            return;
        }

        m_streamVisual = Instantiate(m_streamVisualPrefab, transform.position, transform.rotation, transform);
    }

    private void UpdateStreamSimulation()
    {
        FluidStreamResult result =
            FluidStreamSimulator.Simulate(
                transform.position,
                transform.forward * m_data.launchSpeed,
                Physics.gravity,
                m_data.sampleCount,
                m_data.sampleStep);

        if (m_streamVisual != null)
        {
            m_streamVisual.SetPoints(result);
        }

        // Only ever touches the pour-target, never the formal pipe connection.
        SetDownstreamLeakTarget(result.Receiver);
    }

    private void StopStreamSimulation()
    {
        SetDownstreamLeakTarget(null);
        if (m_streamVisual)
        {
            m_streamVisual.Hide();
        }
    }

    public bool CanInteract(Controller_Equipment controller)
    {
        return true;
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        SetHighlight(CanInteract(controller) ? InteractionHighlightState.VALID : InteractionHighlightState.NONE);
    }

    public void OnHoverExit(Controller_Equipment controller)
    {
        SetHighlight(InteractionHighlightState.NONE);
    }

    public void OnInteract(Controller_Equipment controller)
    {
        this.m_data.m_active = !this.m_data.m_active;
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController)
    {
        SetHighlight(CanInteract(equipmentController) ? InteractionHighlightState.VALID : InteractionHighlightState.NONE);
    }

    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        m_highlight_renderer.SetHighlight(state);
    }

    void IFluidSender.SendFluid(float amount_to_send_L, float dt)
    {
        SendFluid(amount_to_send_L, dt);
    }

    public void RemoveDownstreamLeakTarget(IFluidReceiver target)
    {
        SetDownstreamLeakTarget(null);
    }
}
