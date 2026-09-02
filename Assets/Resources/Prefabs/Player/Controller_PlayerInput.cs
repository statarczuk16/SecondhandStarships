using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Single owner of the PlayerControls input actions instance.
/// </summary>
public class Controller_PlayerInput : MonoBehaviour
{
    public PlayerControls Controls { get; private set; }

    void Awake()
    {
        Controls = new PlayerControls();
    }

    void OnEnable()
    {
        Controls.MovementMode.Enable();
    }

    void OnDisable()
    {
        Controls.MovementMode.Disable();
        Controls.MenuMode.Disable();
    }

    void OnDestroy()
    {
        Controls.Dispose();
    }

    /// <summary>Call when a tool minigame begins. Swaps MovementMode off, MenuMode on.</summary>
    public void EnterMiniGame()
    {
        Controls.MovementMode.Disable();
        Controls.MenuMode.Enable();
    }

    /// <summary>Call when a tool minigame ends. Swaps MenuMode off, MovementMode back on.</summary>
    public void ExitMiniGame()
    {
        Controls.MenuMode.Disable();
        Controls.MovementMode.Enable();
    }
}