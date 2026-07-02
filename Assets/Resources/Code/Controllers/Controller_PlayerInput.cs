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
        Controls.WalkingMode.Enable();
    }

    void OnDisable()
    {
        Controls.WalkingMode.Disable();
        Controls.WorkingMode.Disable();
    }

    void OnDestroy()
    {
        Controls.Dispose();
    }

    /// <summary>Call when a tool minigame begins. Swaps WalkingMode off, WorkingMode on.</summary>
    public void EnterMiniGame()
    {
        Controls.WalkingMode.Disable();
        Controls.WorkingMode.Enable();
    }

    /// <summary>Call when a tool minigame ends. Swaps WorkingMode off, WalkingMode back on.</summary>
    public void ExitMiniGame()
    {
        Controls.WorkingMode.Disable();
        Controls.WalkingMode.Enable();
    }
}