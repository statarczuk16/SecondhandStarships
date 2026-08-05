using UnityEngine;

public class Component_Inventory : MonoBehaviour, IHighlightable, IInventoryOwner, IInteractable
{
    [SerializeField] private Data_Inventory m_data_inventory;
    
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
        controller.DisplayInventory(controller, this);
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController, RaycastHit hitInfo)
    {
        return;
    }

    public string GetInteractionLabel(Controller_Equipment controller)
    {
        return $"//SERVICE HATCH -> OPEN TO INSPECT PARTS";
    }

    public Transform InteractionPoint => this.transform;
    
    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        if (state == InteractionHighlightState.VALID)
        {
            MeshRenderer graphics = this.GetComponent<MeshRenderer>();
            graphics.enabled = true;
        }
        else
        {
            MeshRenderer graphics = this.GetComponent<MeshRenderer>();
            graphics.enabled = false;
        }
    }
}
