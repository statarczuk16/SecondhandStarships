using System.Collections.Generic;
using UnityEngine;



public class Component_ShipPart : MonoBehaviour
{
    [SerializeField] private Data_ShipPart m_data;
    [SerializeField] private InstallationState m_part_state = InstallationState.UNINSTALLED;

    private List<IPartConnector> m_connectors;
    private Component_ShipPartSlot m_parent_slot;

    private void Awake()
    {
        // Interface lists can't be serialized in the Inspector, so gather connectors
        // from the part's own hierarchy at runtime instead.
        m_connectors = new List<IPartConnector>(GetComponentsInChildren<IPartConnector>(true));
        foreach (IPartConnector connector in m_connectors)
        {
            connector.SetOwner(this);
        }
    }

    public Data_ShipPart GetData() => m_data;
    public void SetData(Data_ShipPart data) => m_data = data;
    public ShipSlotSize GetPartSize() => m_data.slot_size;
    public InstallationState GetPartState() => m_part_state;
    public void SetInstallState(InstallationState state) => m_data.install_data = state;

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
        if (m_part_state == InstallationState.INSTALLED)
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
        m_parent_slot = null;
        //TODO do physics stuff
    }

}