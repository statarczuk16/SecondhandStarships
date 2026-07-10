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

    [Header("Movement")]
    [SerializeField] float walkSpeed = 4.5f;
    [SerializeField] float sprintSpeed = 7f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] float groundedStickForce = -2f; // small downward force to keep grounded checks stable

    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float minPitch = -85f;
    [SerializeField] float maxPitch = 85f;

    CharacterController character_controller;
    Controller_PlayerInput inputHub;
    Vector3 velocity;
    float pitch;
    bool jumpQueued;


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
        inputHub.Controls.WalkingMode.Jump.performed += OnJumpPerformed;
    }
    void OnDisable()
    {
        inputHub.Controls.WalkingMode.Jump.performed -= OnJumpPerformed;
    }

    void OnJumpPerformed(InputAction.CallbackContext ctx) => jumpQueued = true;

    void Update()
    {
        // If WalkingMode map is disabled (we're in a tool minigame), skip locomotion entirely.
        if (!inputHub.Controls.WalkingMode.enabled) return;

        HandleLook();
        HandleMove();
    }

    void HandleLook()
    {
        Vector2 lookDelta = inputHub.Controls.WalkingMode.Look.ReadValue<Vector2>() * mouseSensitivity * 0.1f;

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

        Vector2 moveInput = inputHub.Controls.WalkingMode.Move.ReadValue<Vector2>();
        Vector3 moveDir = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

        bool sprinting = inputHub.Controls.WalkingMode.Sprint.IsPressed();
        float speed = sprinting ? sprintSpeed : walkSpeed;
        character_controller.Move(moveDir * speed * Time.deltaTime);

        if (isGrounded && jumpQueued)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        jumpQueued = false;

        velocity.y += gravity * Time.deltaTime;
        character_controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>Call from WorkingModeController alongside PlayerInputHub.EnterWorkingMode().</summary>
    public void OnEnterMiniGame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Call from WorkingModeController alongside PlayerInputHub.ExitWorkingMode().</summary>
    public void OnExitMiniGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}