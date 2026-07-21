using TMPro;
using UnityEngine;

public enum InteractionHighlightState
{
    VALID,
    INVALID,
    NONE
}

public interface IInteractable
{
    bool CanInteract(Controller_Equipment controller);
    void OnHoverEnter(Controller_Equipment controller);
    void OnHoverExit(Controller_Equipment controller);
    void OnInteract(Controller_Equipment controller); // called on click, triggers working mode
    void OnHoverUpdate(Controller_Equipment equipmentController,  RaycastHit hitInfo);

    Transform InteractionPoint { get; } // camera focus point for minigame framing
}

public interface IHighlightable
{
    void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null); // None, Valid, Invalid
}