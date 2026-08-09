using System;
using UnityEngine;

public class Component_Inventory : MonoBehaviour, IHighlightable, IInventoryOwner, IInteractable
{
    [SerializeField] private Data_Inventory m_data_inventory;
    [SerializeField] private Component_PrefabBoundary m_owning_prefab;
    [SerializeField] private IInventoryOwner m_owner;
    [SerializeField] private HighlightableRenderer m_highlightable;

    private void Awake()
    {
        if (!m_owning_prefab)
        {
            throw new Exception("Component_PrefabBoundary not found " + this.gameObject.name);
        }
        m_owner = m_owning_prefab.GetComponent<IInventoryOwner>();
       
    }

    public Data_Inventory GetInventory()
    {
        return this.m_data_inventory;
    }
    public bool IsInstallTarget()
    {
        return m_owner?.IsInstallTarget() ?? false;
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
        bool visible = state == InteractionHighlightState.VALID;
        if (m_highlightable)
        {
            m_highlightable.SetHighlight(state);
        }
        else
        {
            MeshRenderer[] graphics = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].enabled = visible;
            }
        }
        
    }
}
