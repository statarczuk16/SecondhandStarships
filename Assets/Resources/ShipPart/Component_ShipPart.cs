using System;
using System.Collections.Generic;
using UnityEngine;

public class Component_ShipPart : MonoBehaviour
{
    [SerializeField] private Data_ShipPart m_data;

    public Data_ShipPart GetData()
    {
        return m_data;
    }

    public void SetData(Data_ShipPart data)
    {
        this.m_data = data;
    }


    public ShipSlotSize GetPartSize()
    {
        return m_data.slot_size;
    }

    public void NotifyAttachmentCleared(IAttachmentSlot cleared)
    {
        
    }

    void Detach()
    {
        m_data.is_installed = false;
        // enable Rigidbody + Collider for pickup, disable fastener interactables,
        // reparent out of the ship hierarchy, fire event for inventory/quest hooks
        
    }

    internal void SetInstalled(Component_ShipPartSlot component_ShipPartSlot)
    {
        m_data.is_installed = true;
    }
}