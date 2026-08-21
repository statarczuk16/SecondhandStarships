using UnityEngine;

public class Component_PowerNetworkInfoScreen : MonoBehaviour, IInteractable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private string power_network_status = "//POWER NETWORK STATUS:\n//ERROR NO NETWORK CONNECTED";
    [SerializeField, Required] private Component_PowerNetwork m_connected_network;
    public bool CanInteract(Controller_Equipment controller)
    {
        return true;
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        
    }

    public void OnHoverExit(Controller_Equipment controller)
    {
        
    }

    public void OnInteract(Controller_Equipment controller)
    {
        
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController, RaycastHit hitInfo)
    {
        
    }

    public string GetInteractionLabel(Controller_Equipment controller)
    {
        return m_connected_network.ToString();
    }

    public Transform InteractionPoint { get; }
}
