using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Raycasts from the camera each frame to find IInteractable targets,
/// handles hover enter/exit, and fires OnInteract on click (MovementMode/Interact).
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
    IInteractable m_current_interactable;

    void Awake()
    {
        equipmentController = GetComponent<Controller_Equipment>();
        player_input_controller = GetComponent<Controller_PlayerInput>();
        int ghostLayer = LayerMask.NameToLayer("Ghost");
        if (ghostLayer != -1)
        {
            interactableMask &= ~(1 << ghostLayer);
        }
    }

    private void Start()
    {   
        //When this component is enabled, we add an event listener, so OnInteractPerformed is called whenever the player controls press/perform the interact button
        player_input_controller.Controls.MovementMode.Interact.performed += OnInteractPerformed;
        player_input_controller.Controls.MovementMode.AltInteract.performed += OnAltInteractPerformed;
        player_input_controller.Controls.MovementMode.Scroll.performed += OnScrollPerformed;
        player_input_controller.Controls.MovementMode.OpenPlayerMenu.performed += OnOpenPlayerMenuPerformed;
        player_input_controller.Controls.MenuMode.Cancel.performed += OnClosePlayerMenuPerformed;
    }

    private void OnOpenPlayerMenuPerformed(InputAction.CallbackContext obj)
    {
        equipmentController.OpenPlayerMenu();
    }
    
    private void OnClosePlayerMenuPerformed(InputAction.CallbackContext obj)
    {
        equipmentController.ClosePlayerMenu();
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
        
    }

    void OnDisable()
    {
        //and remove that event listener when the component is disabled
        //player_input_controller.Controls.MovementMode.Interact.performed -= OnInteractPerformed;
    }

    void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        TryInteract();
    }
    
    void OnAltInteractPerformed(InputAction.CallbackContext ctx)
    {
        TryAltInteract();
    }

    void Update()
    {
        // Don't hover-scan while a minigame owns input.
        if (!player_input_controller.Controls.MovementMode.enabled) return;

        UpdateHoverTarget();
    }

    void UpdateHoverTarget()
    {
        MonoBehaviour mb = null;
        IInteractable interactable = null;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out RaycastHit hitInfo, interactRange, interactableMask))
        {
            //check for interactable on thing we are looking at
            if (hitInfo.collider.gameObject.TryGetComponent<IInteractable>(out interactable))
            {
                mb = interactable as MonoBehaviour;
            }
            //if nothing there, check if the owning prefab of this object has an interactable
            else
            {
                Transform boundaryRoot = ShipPartUtilities.FindOwningPrefabBoundary(hitInfo.collider.transform);
                if (boundaryRoot)
                {
                    interactable = ShipPartUtilities.FindComponentWithinPrefab<IInteractable>(boundaryRoot);
                    mb = interactable as MonoBehaviour;
                }
            }
        }

        if (!mb)
        {
            coyote_time_frame_count += 1;
            if (coyote_time_frame_count >= COYOTE_TIME_FRAME_TARGET)
            {
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

        if (interactable == m_current_interactable)
        {
            equipmentController.OnHoverUpdate(m_current_interactable, hitInfo);
            return;
        }

        equipmentController.OnHoverExit(m_current_interactable);
        m_current_interactable = interactable;
        equipmentController.setCurrentInteractable(m_current_interactable, hitInfo);
        equipmentController.OnHoverEnter(m_current_interactable);
        
    }

    void TryAltInteract()
    {
        equipmentController.ActivateTool();
    }
    
    void TryInteract()
    {
        //Call the OnInteract function of whatever we are looking at (currentTarget)
        if (m_current_interactable == null) return;
        if (!m_current_interactable.CanInteract(equipmentController)) return;

        m_current_interactable.OnInteract(equipmentController);
    }

    
}