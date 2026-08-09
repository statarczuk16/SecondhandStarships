using UnityEngine;

public class Component_Inventory_Accessor : MonoBehaviour, IHighlightable, IInventoryOwner, IInteractable
{
    [SerializeField, Required] private Component_Inventory m_linked_inventory;
    [SerializeField, Required] private HighlightableRenderer m_highlightable;
    
    
    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        m_highlightable.SetHighlight(state);
    }
    
    public Data_Inventory GetInventory()
    {
        return m_linked_inventory.GetInventory();
    }

    public bool IsInstallTarget()
    {
        return m_linked_inventory.IsInstallTarget();
    }

    public bool CanInteract(Controller_Equipment controller)
    {
        return m_linked_inventory.CanInteract(controller);
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
        m_linked_inventory.OnInteract(controller);
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController, RaycastHit hitInfo)
    {
        return;
    }

    public string GetInteractionLabel(Controller_Equipment controller)
    {
        return m_linked_inventory.GetInteractionLabel(controller);
    }

    public Transform InteractionPoint { get; }
}