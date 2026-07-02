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
    void OnHoverExit();
    void OnInteract(Controller_Equipment controller); // called on click, triggers working mode
    Transform InteractionPoint { get; } // camera focus point for minigame framing
}

public interface IHighlightable
{
    void SetHighlight(InteractionHighlightState state); // None, Valid, Invalid
}