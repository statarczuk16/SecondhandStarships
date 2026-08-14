using UnityEngine;

public class Connector_InventoryItemRemoval : IPartConnector
{
     private int m_installation_progress = 0; // 0 = loose, 100 = fully tight
     private InstallationState m_finish_state;
     private InstallationState m_installation_state = InstallationState.UNINSTALLED;
     private EquipmentType m_required_tool;

     public Connector_InventoryItemRemoval(InstallationState start_state, EquipmentType required_tool)
    {   
       
        m_installation_state = start_state;
        m_required_tool = required_tool;
        if (start_state == InstallationState.UNINSTALLED)
        {
            m_finish_state = InstallationState.UNINSTALLED;
            m_installation_progress = 0;
        }
        else
        {
            m_finish_state =  InstallationState.INSTALLED;
            m_installation_progress = 100;
        }
        
    }

    public InstallationState GetInstallState() => m_installation_state;

    public int GetInstallationProgress() => m_installation_progress;
        

    public void InstallationUpdate(int amount)
    {
        m_installation_progress = Mathf.Clamp(m_installation_progress + amount, 0, 100);
        InstallationState prev_state = m_installation_state;
        if (m_installation_progress <= 0)
        {
            m_installation_state = InstallationState.UNINSTALLED;
        }
        else if (m_installation_progress >= 100)
        {
            m_installation_state = InstallationState.INSTALLED;
        }
        else
        {
            m_installation_state = InstallationState.INSTALLING;
        }

        if (m_finish_state == m_installation_state)
        {
            
        }
    }

    public EquipmentType RequiredTool() => m_required_tool;
    public bool SetOwner(Component_ShipPart owner) => false; // not applicable here
    public void InitializeConnector() { }

    public void ForceInstall()
    {
        m_finish_state = InstallationState.INSTALLED;
        m_installation_progress = 100;
    }
}