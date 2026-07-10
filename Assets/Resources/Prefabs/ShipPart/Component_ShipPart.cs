using System;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(HighlightableRenderer))]

public class Component_ShipPart : MonoBehaviour, IInteractable, IHighlightable
{
    [SerializeField] private Data_ShipPart m_data;
    [SerializeField] private List<IPartConnector> m_connectors;
    private Component_ShipPartSlot m_parent_slot;
    private HighlightableRenderer m_highlight_renderer;
    public Transform InteractionPoint => throw new NotImplementedException();

    private void Awake()
    {
        // Interface lists can't be serialized in the Inspector, so gather connectors
        // from the part's own hierarchy at runtime instead.
        m_connectors = new List<IPartConnector>(GetComponentsInChildren<IPartConnector>(true));
        foreach (IPartConnector connector in m_connectors)
        {
            connector.SetOwner(this);
        }
        m_highlight_renderer = this.GetComponent<HighlightableRenderer>();
    }

    public Data_ShipPart GetData() => m_data;
    public void SetData(Data_ShipPart data) => m_data = data;
    public ShipSlotSize GetPartSize() => m_data.slot_size;
    public InstallationState GetPartState() => m_data.install_state;
    public void SetInstallState(InstallationState state) => m_data.install_state = state;

    internal void StartInstall(Component_ShipPartSlot slot)
    {
        SetInstallState(InstallationState.INSTALLING);

        foreach (IPartConnector connector in m_connectors)
        {
            connector.InitializeConnector();
        }
        m_parent_slot = slot;
    }

    public void NotifyConnectorUninstalled(IPartConnector connector)
    {
        if (m_data.install_state == InstallationState.INSTALLED)
        {
            SetInstallState(InstallationState.INSTALLING);
        }

        if (m_connectors.TrueForAll(c => c.GetInstallState() == InstallationState.UNINSTALLED))
        {
            Detach();
        }
    }

    public void NotifyConnectorInstalled(IPartConnector connector)
    {
        if (m_connectors.TrueForAll(c => c.GetInstallState() == InstallationState.INSTALLED))
        {
            SetInstallState(InstallationState.INSTALLED);
        }
    }

    public void Detach()
    {
        SetInstallState(InstallationState.UNINSTALLED);
        m_parent_slot.NotifyOfPartDisconnect();
        m_parent_slot = null;
        //TODO do physics stuff
    }

    internal void NotifyConnectorInstalling(IPartConnector connector)
    {
        SetInstallState(InstallationState.INSTALLING);
    }

    public bool CanInteract(Controller_Equipment controller)
    {
        return this.m_data.install_state == InstallationState.UNINSTALLED || this.m_data.install_state == InstallationState.INSTALLED;
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
        if(this.m_data.install_state == InstallationState.UNINSTALLED)
        {
            controller.AddPartToInventory(this);
            GameObject.Destroy(this);
        }
        else if(this.m_data.install_state == InstallationState.INSTALLED)
        {
            Debug.Log("Interacted with installed thing!");
        }
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController)
    {
        
    }

    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        m_highlight_renderer.SetHighlight(state);
    }
}