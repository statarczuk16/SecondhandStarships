using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Basic first-person locomotion: WASD movement, mouse look, jump, gravity.
/// Reads input from the shared PlayerControls instance owned by PlayerInputHub.
/// Requires a CharacterController on the same GameObject.
/// Assign a child camera transform (positioned at eye height) to camTransform.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Controller_PlayerInput))]
public class Controller_PlayerFPSMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform camTransform;
    [SerializeField] Transform playerGraphicTransform;

    [Header("Movement")]
    [SerializeField] float walkSpeed = 4.5f;
    [SerializeField] float sprintSpeed = 7f;
    [SerializeField] float gravity = -25f;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] float groundedStickForce = -2f; // small downward force to keep grounded checks stable
    
    [Header("Crouch Settings")]
    [SerializeField] float normalHeight = 2.0f;
    [SerializeField] float crouchHeight = 1.0f;
    [SerializeField] float crouchTransitionSpeed = 10f;
    [SerializeField] float normalCameraY = 0.6f;
    [SerializeField] float crouchCameraY = 0.2f;
    
    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float minPitch = -85f;
    [SerializeField] float maxPitch = 85f;

    CharacterController character_controller;
    Controller_PlayerInput inputHub;
    Vector3 velocity;
    float pitch;
    bool jumpQueued;
    bool isCrouching;
    


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Awake()
    {
        character_controller = GetComponent<CharacterController>();
        inputHub = GetComponent<Controller_PlayerInput>();
    }
    void OnEnable()
    {
        inputHub.Controls.MovementMode.Jump.performed += OnJumpPerformed;
        inputHub.Controls.MovementMode.Crouch.performed += OnCrouchPerformed;
    }
    void OnDisable()
    {
        inputHub.Controls.MovementMode.Jump.performed -= OnJumpPerformed;
        inputHub.Controls.MovementMode.Crouch.performed -= OnCrouchPerformed;
    }

    void OnJumpPerformed(InputAction.CallbackContext ctx) => jumpQueued = true;
    
    void OnCrouchPerformed(InputAction.CallbackContext ctx) => ToggleCrouch();

    void Update()
    {
        // If MovementMode map is disabled (we're in a tool minigame), skip locomotion entirely.
        if (!inputHub.Controls.MovementMode.enabled) return;

        HandleLook();
        HandleMove();
        HandleCrouchTransition();
    }

    void ToggleCrouch()
    {
        isCrouching = !isCrouching;
    }
    
    void HandleCrouchTransition()
    {
        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        float targetCenterY = isCrouching ? (crouchHeight / 2.0f) : (normalHeight / 2.0f);

        // 1. Smoothly interpolate Character Controller height & center
        character_controller.height = Mathf.Lerp(character_controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        
        Vector3 currentCenter = character_controller.center;
        currentCenter.y = Mathf.Lerp(currentCenter.y, targetCenterY, Time.deltaTime * crouchTransitionSpeed);
        character_controller.center = currentCenter;

        // 2. Smoothly interpolate Camera local Y position
      
        float targetCameraY = isCrouching ? crouchCameraY : normalCameraY;
        Vector3 camPos = camTransform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCameraY, Time.deltaTime * crouchTransitionSpeed);
        camTransform.localPosition = camPos;
    
    
   
        // Standard Unity primitive capsules have a default height of 2 units when localScale.y is 1.
        // Therefore, localScale.y should equal the current controller height divided by 2 (or just matched directly if your mesh is 1 unit tall).
        Vector3 graphicScale = playerGraphicTransform.localScale;
        graphicScale.y = character_controller.height / 2.0f; // Adjust this divisor if your base mesh height differs from 2
        playerGraphicTransform.localScale = Vector3.Lerp(playerGraphicTransform.localScale, graphicScale, Time.deltaTime * crouchTransitionSpeed);

        // Match the graphic's local position to the controller's center
        Vector3 graphicPos = playerGraphicTransform.localPosition;
        graphicPos.y = character_controller.center.y;
        playerGraphicTransform.localPosition = Vector3.Lerp(graphicPos, graphicPos, Time.deltaTime * crouchTransitionSpeed); // (Alternatively, just assign it directly since the controller is already lerped)
        playerGraphicTransform.localPosition = new Vector3(graphicPos.x, character_controller.center.y, graphicPos.z);
        
    }

    void HandleLook()
    {
        Vector2 lookDelta = inputHub.Controls.MovementMode.Look.ReadValue<Vector2>() * mouseSensitivity * 0.1f;

        // Yaw rotates the whole body, pitch rotates only the camera.
        transform.Rotate(Vector3.up * lookDelta.x);

        pitch -= lookDelta.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        camTransform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    void HandleMove()
    {
        bool isGrounded = character_controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
            velocity.y = groundedStickForce;

        Vector2 moveInput = inputHub.Controls.MovementMode.Move.ReadValue<Vector2>();
        Vector3 moveDir = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

        bool sprinting = inputHub.Controls.MovementMode.Sprint.IsPressed();
        float speed = sprinting ? sprintSpeed : walkSpeed;
        character_controller.Move(moveDir * speed * Time.deltaTime);

        if (isGrounded && jumpQueued)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        jumpQueued = false;

        velocity.y += gravity * Time.deltaTime;
        character_controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>Call from MenuModeController alongside PlayerInputHub.EnterMenuMode().</summary>
    public void OnEnterMiniGame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Call from MenuModeController alongside PlayerInputHub.ExitMenuMode().</summary>
    public void OnExitMiniGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}