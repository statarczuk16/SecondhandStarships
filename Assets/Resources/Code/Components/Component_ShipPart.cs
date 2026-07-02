using System;
using System.Collections.Generic;
using UnityEngine;

public class Component_ShipPart : MonoBehaviour
{
    List<IAttachmentSlot> m_attachment_slots;
    public bool IsDetached { get; private set; }

    void Awake()
    {
        foreach (var a in m_attachment_slots) a.SetOwner(this);
    }

    public void NotifyAttachmentCleared(IAttachmentSlot cleared)
    {
        bool any_still_fastened = false;
        foreach(IAttachmentSlot slot in m_attachment_slots)
        {
            if(slot.FastenerInstalled)
            {
                any_still_fastened = true;
                break;
            }
        }
        if(any_still_fastened == false)
        {
            Debug.Log("Part is detached!");
            Detach();
        }
    }

    void Detach()
    {
        IsDetached = true;
        // enable Rigidbody + Collider for pickup, disable fastener interactables,
        // reparent out of the ship hierarchy, fire event for inventory/quest hooks
        OnPartDetached?.Invoke(this);
    }

    public event Action<Component_ShipPart> OnPartDetached;
}