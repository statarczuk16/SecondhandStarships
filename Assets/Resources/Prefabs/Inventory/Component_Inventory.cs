using UnityEngine;

[RequireComponent(typeof(HighlightableRenderer))] 
public class Component_Inventory : MonoBehaviour, IHighlightable, IInventoryOwner, IInteractable
{
    [SerializeField] private Data_Inventory m_data_inventory;
    [SerializeField] private HighlightableRenderer m_highlight_renderer;
    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        m_highlight_renderer.SetHighlight(state);
    }

    public Data_Inventory GetInventory()
    {
        return this.m_data_inventory;
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
        controller.DisplayInventory(this);
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController, RaycastHit hitInfo)
    {
        return;
    }

    public Transform InteractionPoint => this.transform;
}
