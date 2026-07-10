using UnityEngine;
using UnityEngine.InputSystem.XR;

public class Component_HullSlot : MonoBehaviour, IInteractable, IHighlightable
{
    [SerializeField] private GameObject defaultHullPartPrefab;
    bool installed = false;

    public Transform InteractionPoint => transform;

    public void Awake()
    {
        //So we can see the green preview in the scene editor but it goes away during the game
        MeshRenderer graphics = this.GetComponent<MeshRenderer>();
        graphics.enabled = false;
    }

    public bool CanInteract(Controller_Equipment controller)
    {
        //can only install here if nothing installed yet
        return installed == false;
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        //turn on the green preview ghost when we hover mouse over 
        if(CanInteract(null))
        {
            SetHighlight(InteractionHighlightState.VALID);
        }
    }


    public void OnHoverExit(Controller_Equipment controller)
    {
        SetHighlight(InteractionHighlightState.NONE);
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController)
    {
        
    }

    public void OnInteract(Controller_Equipment controller)
    {
        if(CanInteract(null))
        {
            MeshRenderer graphics = this.GetComponent<MeshRenderer>();
            graphics.enabled = false;
            GameObject spawnedPart = GameObject.Instantiate(defaultHullPartPrefab, this.transform.parent, false);
            spawnedPart.transform.localPosition = Vector3.zero;
            installed = true;
        }
    }


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
