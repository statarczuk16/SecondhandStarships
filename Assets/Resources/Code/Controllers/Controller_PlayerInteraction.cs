using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Raycasts from the camera each frame to find IInteractable targets,
/// handles hover enter/exit, and fires OnInteract on click (WalkingMode/Interact).
/// Reads input from the shared PlayerControls instance owned by PlayerInputHub.
/// </summary>
[RequireComponent(typeof(Controller_Equipment))]
[RequireComponent(typeof(Controller_PlayerInput))]
public class Controller_PlayerInteraction : MonoBehaviour
{
    [SerializeField] Camera playerCamera;
    [SerializeField] float interactRange = 3f;
    [SerializeField] LayerMask interactableMask = ~0;

    Controller_Equipment equipmentController;
    Controller_PlayerInput player_input_controller;
    IInteractable currentTarget;

    void Awake()
    {
        equipmentController = GetComponent<Controller_Equipment>();
        player_input_controller = GetComponent<Controller_PlayerInput>();
    }

    private void Start()
    {   
        //When this component is enabled, we add an event listener, so OnInteractPerformed is called whenever the player controls press/perform the interact button
        player_input_controller.Controls.WalkingMode.Interact.performed += OnInteractPerformed;
    }

    void OnDisable()
    {
        //and remove that event listener when the component is disabled
        //player_input_controller.Controls.WalkingMode.Interact.performed -= OnInteractPerformed;
    }

    void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        TryInteract();
    }

    void Update()
    {
        // Don't hover-scan while a minigame owns input.
        if (!player_input_controller.Controls.WalkingMode.enabled) return;

        UpdateHoverTarget();
    }

    void UpdateHoverTarget()
    {
        
        IInteractable hit = null;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out RaycastHit hitInfo, interactRange, interactableMask))
        {
            hit = hitInfo.collider.GetComponentInParent<IInteractable>();
        }

        if (hit == currentTarget) return;

        currentTarget?.OnHoverExit();
        currentTarget = hit;
        currentTarget?.OnHoverEnter(equipmentController);
    }

    void TryInteract()
    {
        //Call the OnInteract function of whatever we are looking at (currentTarget)
        if (currentTarget == null) return;
        if (!currentTarget.CanInteract(equipmentController)) return;

        currentTarget.OnInteract(equipmentController);
    }
}