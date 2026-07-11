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


    internal void StartInstall(Component_ShipPartSlot slot)
    {
        m_data.install_state = InstallationState.INSTALLING;
        m_parent_slot = slot;

        
        foreach (IPartConnector connector in m_connectors)
        {
            connector.InitializeConnector();
        }
        
        if(PartUsesConnectors() == false)
        {
            OnInstalled();
        }
        AudioEvents.Fire(SoundID.Part_Placed, this.transform.position);


    }

    public void OnConnectorStatusChanged()
    {
        int num_connectors = m_connectors.Count;
        int num_connectors_installed = 0;
        int num_connectors_installing = 0;
        int num_connectors_uninstalled = 0;
        foreach (IPartConnector connector in m_connectors)
        {
            if(connector.GetInstallState() == InstallationState.INSTALLED)
            {
                num_connectors_installed += 1;
            }
            else if(connector.GetInstallState() == InstallationState.INSTALLING)
            {
                num_connectors_installing += 1;
            }
            else
            {
                num_connectors_uninstalled += 1;
            }
        }
        if(num_connectors_installed == num_connectors)
        {
            OnInstalled();
        }
        else if(num_connectors_uninstalled == num_connectors)
        {
            OnUninstalled();
        }
        else
        {
            OnInstalling();
        }
    }

    private bool PartUsesConnectors()
    {
        return m_connectors.Count > 0;
    }

    private void OnInstalling()
    {
        if (this.m_data.install_state == InstallationState.INSTALLING)
        {
            return; //no op we are already 
        }
        this.m_data.install_state = InstallationState.INSTALLING;
        this.m_parent_slot.OnPartUninstalled(this);
    }

    private void OnInstalled()
    {
        if(this.m_data.install_state == InstallationState.INSTALLED)
        {
            return; //no op we are already 
        }
        AudioEvents.Fire(SoundID.Part_Installed, this.transform.position);
        this.m_data.install_state = InstallationState.INSTALLED;
        this.m_parent_slot.OnPartInstalled(this);
    }

    private void OnUninstalled()
    {
        if (this.m_data.install_state == InstallationState.UNINSTALLED)
        {
            return; //no op we are already
        }
        this.m_data.install_state = InstallationState.UNINSTALLED;
        this.m_parent_slot.OnPartUninstalled(this);
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
        if(this.m_data.install_state == InstallationState.UNINSTALLED || PartUsesConnectors() == false)
        {
            OnUninstalled();
            this.m_parent_slot = null;
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