using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public enum ShipSlotSize
{
    TINY,
    SMALL,
    MEDIUM,
    LARGE,
    XLARGE
}

public class Component_ShipPartSlot : MonoBehaviour, IInteractable, IHighlightable
{
    [SerializeField] private Data_ShipPartSlot m_data;

    private Component_ShipPart m_installed_part;
    private GameObject currentGhost;

    public Transform InteractionPoint => transform;
    public string SlotId => m_data.guid.ToString();
    public bool IsOccupied => m_installed_part != null;
    public Component_ShipPart InstalledPart => m_installed_part;

    public bool CanInteract(Controller_Equipment controller)
    {
        if (m_data.filled)
        {
            return false;
        }
        if (this.PartIsCompatibleWithMe(controller.GetEquippedPart()) == false)
        {
            return false;
        }
        return true;
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        List<Data_ShipPart> all_player_parts = controller.GetShipPartInventory();
        List<Data_ShipPart> compatible_parts = new List<Data_ShipPart>();
        foreach (Data_ShipPart data in all_player_parts)
        {
            if (PartIsCompatibleWithMe(data))
            {
                compatible_parts.Add(data);
            }
        }
        controller.SetRelevantShipParts(compatible_parts);

        if (CanInteract(controller))
        {
            SetHighlight(InteractionHighlightState.VALID, controller);
        }
    }

    public void OnHoverExit(Controller_Equipment controller)
    {
        SetHighlight(InteractionHighlightState.NONE, controller);
    }

    public void OnHoverUpdate(Controller_Equipment controller)
    {
        if (CanInteract(controller))
        {
            ClearGhost();
            SetHighlight(InteractionHighlightState.VALID, controller);
        }
    }

    public void OnInteract(Controller_Equipment controller)
    {
        if (CanInteract(controller))
        {
            ClearGhost();
            Data_ShipPart data = controller.GetEquippedPart();
            Component_MountPoint mount = this.GetComponentInChildren<Component_MountPoint>();

            GameObject spawnedPart = Instantiate(data.prefab, mount.transform, false);
            spawnedPart.transform.localPosition = Vector3.zero;

            Component_ShipPart part = spawnedPart.GetComponent<Component_ShipPart>();
            part.SetData(data);

            m_installed_part = part;
            m_data.filled = true;

            controller.RemovePartFromInventory(data);
        }
    }

    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller)
    {
        try
        {
            if (state == InteractionHighlightState.VALID)
            {
                if (currentGhost == null)
                {
                    Component_MountPoint mount = this.GetComponentInChildren<Component_MountPoint>();
                    currentGhost = GhostPreviewFactory.CreateGhost(controller.GetEquippedPart().prefab, mount.transform, Color.green);
                }
            }
            else
            {
                ClearGhost();
            }
        }
        catch (Exception e)
        {
            TopicLogger.Log(LogTopic.Interaction, LogLevel.ERROR, $"Error with {this.name} {e.Message} {e.StackTrace}");
        }
    }

    private void ClearGhost()
    {
        if (currentGhost != null)
        {
            GhostPreviewFactory.Destroy(currentGhost);
            currentGhost = null;
        }
    }

    private bool PartIsCompatibleWithMe(Component_ShipPart part)
    {
        if (part.GetPartSize() > m_data.max_allowed_size)
        {
            return false;
        }
        return true;
    }

    private bool PartIsCompatibleWithMe(Data_ShipPart data)
    {
        if(data == null)
        {
            return false;
        }
        if (data.slot_size > m_data.max_allowed_size)
        {
            return false;
        }
        return true;
    }
}