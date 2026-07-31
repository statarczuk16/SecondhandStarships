using System;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(HighlightableRenderer))]
[RequireComponent(typeof(Component_PrefabBoundary))]
public class Component_ShipPart : MonoBehaviour, IInteractable, IHighlightable
{
    [SerializeField] private Data_ShipModule m_data;
    [SerializeField] private List<IPartConnector> m_connectors;
    [SerializeField] private Component_BuildableSurface m_parent_surface;
    [SerializeField] private Data_Inventory mDataInventory;
    
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

    public Data_ShipModule GetData() => m_data;
    public void SetData(Data_ShipModule data) => m_data = data;
    public InstallationState GetPartState() => m_data.install_state;


    internal void StartInstall(Component_BuildableSurface surface)
    {
        m_data.install_state = InstallationState.UNINSTALLED;
        m_parent_surface = surface;
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
        this.m_parent_surface.OnPartUninstalled(this);
    }

    private void OnInstalled()
    {
        if(this.m_data.install_state == InstallationState.INSTALLED)
        {
            return; //no op we are already 
        }
        AudioEvents.Fire(SoundID.Part_Installed, this.transform.position);
        this.m_data.install_state = InstallationState.INSTALLED;
        this.m_parent_surface.OnPartInstalled(this);
    }

    private void OnUninstalled()
    {
        if (this.m_data.install_state == InstallationState.UNINSTALLED)
        {
            return; //no op we are already
        }
        this.m_data.install_state = InstallationState.UNINSTALLED;
        this.m_parent_surface.OnPartUninstalled(this);
        this.m_parent_surface = null;
    }


    public bool CanInteract(Controller_Equipment controller)
    {
        if (controller.BuildMode())
        {
            return this.m_data.install_state == InstallationState.UNINSTALLED || this.m_data.install_state == InstallationState.INSTALLED;
        }

        return false;
    }
    
    public void OnInteract(Controller_Equipment controller)
    {
        if (controller.BuildMode())
        {
            if(this.m_data.install_state == InstallationState.UNINSTALLED || PartUsesConnectors() == false)
            {
                if (controller.TryAddPartToInventory(this))
                {
                    OnUninstalled();
                    GameObject.Destroy(this.gameObject);
                }
            }
            else if(this.m_data.install_state == InstallationState.INSTALLED)
            {
                Debug.Log("Interacted with installed thing!");
            }
        }
        
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        SetHighlight(CanInteract(controller) ? InteractionHighlightState.VALID : InteractionHighlightState.NONE);

    }

    public void OnHoverExit(Controller_Equipment controller)
    {
        SetHighlight(InteractionHighlightState.NONE);
    }

   

    public void OnHoverUpdate(Controller_Equipment equipmentController, RaycastHit hitInfo)
    {
        
    }

    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        m_highlight_renderer.SetHighlight(state);
    }
}