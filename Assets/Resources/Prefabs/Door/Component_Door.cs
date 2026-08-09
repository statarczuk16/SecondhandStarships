using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Component_Door : MonoBehaviour, IInteractable, IToggleable, IInventoryOwner
{
    [SerializeField, Required] private List<DOTweenAnimation> doorAnimations;
    [SerializeField, Required] private Component_Inventory m_inventory;
    private bool isOpen;

    public void Awake()
    {
        this.m_inventory.ClaimInventory(this);
    }

    public bool CanToggle(out string reason)
    {
        reason = "Whatever";
        if (this.GetInventory().HasAllRecipeItems())
        {
            return true;
        }
        else
        {
            reason = "//ERROR: DOOR MALFUNCTION > CHECK SERVICE HATCH";
            return false;
        }
        
    }

    public void Toggle()
    {
        
        if (isOpen)
        {
            AudioEvents.Fire(SoundID.DoorClose, transform.position);
            foreach (var animation in doorAnimations)
            {
                animation.DOPlayBackwards();
            }
        }
        else
        {
            AudioEvents.Fire(SoundID.DoorOpen, transform.position);
            foreach (var animation in doorAnimations)
            {
                animation.DOPlayForward();
            }
        }
        

        isOpen = !isOpen;
    }

    public bool IsOn()
    {
        return isOpen;
    }

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
        this.Toggle();
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController, RaycastHit hitInfo)
    {
        
    }

    public string GetInteractionLabel(Controller_Equipment controller)
    {
        if (isOpen)
        {
            return "Close Door";
        }
        else
        {
            return "Open Door";
        }
        
    }

    public Transform InteractionPoint { get; }
    public Data_Inventory GetInventory()
    {
        return this.m_inventory.GetInventory();
    }

    public bool IsInstallTarget()
    {
        return true;
    }
}