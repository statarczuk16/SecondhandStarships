using System;
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

    private int coyote_time_frame_count;
    private const int COYOTE_TIME_FRAME_TARGET = 5;
    private GameObject ghostRoot;
    private Controller_Equipment equipmentController;
    private Controller_PlayerInput player_input_controller;
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
        player_input_controller.Controls.WalkingMode.Scroll.performed += OnScrollPerformed;
    }

    private void OnScrollPerformed(InputAction.CallbackContext context)
    {
        Vector2 scroll = context.ReadValue<Vector2>();

        float scrollUpDown = scroll.y;

        if (scrollUpDown > 0)
        {
            equipmentController.ScrollDown();
        }
        else if (scrollUpDown < 0)
        {
            equipmentController.ScrollUp();
        }

        currentTarget?.OnHoverUpdate(equipmentController);
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
            Transform boundaryRoot = ShipPartUtilities.FindOwningPrefabBoundary(hitInfo.collider.transform);
            
            
            if (boundaryRoot != null)
            {
                //Debug.Log($"Top Level Prefab: {boundaryRoot.name}");
                hit = ShipPartUtilities.FindInteractableWithinBoundary(boundaryRoot);
                MonoBehaviour mb = hit as MonoBehaviour;
                if(mb != null)
                {
                    //Debug.Log($"Interactable within: {mb.name}");
                }
                else
                {
                    //Debug.Log($"No interactable on {boundaryRoot.name}");
                }
                
            }
            else
            {
                //Debug.Log($"No prefab boundary on {hitInfo.collider.transform.name}");
                
            }
        }
        //Debug.Log($"Hit {hit}");
        if(hit == null)
        {
            coyote_time_frame_count += 1;
            if(coyote_time_frame_count >= COYOTE_TIME_FRAME_TARGET)
            {
                //Debug.Log($"Player looked at nothing for 5 frames, setting target to null");
                coyote_time_frame_count = COYOTE_TIME_FRAME_TARGET;
            }
            else
            {
                return;
            }
        }
        else
        {
            coyote_time_frame_count = 0;
        }
        if (hit == currentTarget) return;
        //Debug.Log($"New hit! Old {currentTarget} New {hit}");
        equipmentController.setCurrentHover(hit);
        currentTarget?.OnHoverExit(equipmentController);
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