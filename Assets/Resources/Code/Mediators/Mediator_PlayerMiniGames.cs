using System;
using UnityEngine;

public enum PlayerState { MovingMode, WorkingMode }

/// <summary>
/// Conductor for the player's input state. Owns the transition between free-roam
/// and tool minigames, and pokes every dependent system so they change together
/// instead of drifting out of sync.
/// </summary>
[RequireComponent(typeof(Controller_PlayerInput))]
[RequireComponent(typeof(Controller_PlayerFPSMovement))]
public class Mediator_PlayerMiniGames : MonoBehaviour
{
    public PlayerState State { get; private set; } = PlayerState.MovingMode;

    Controller_PlayerInput inputHub;
    Controller_PlayerFPSMovement fpsController;
    [SerializeField] MiniGame_Wrench_UI_Script wrench_minigame_ui;
    IToolMinigame activeMinigame;

    void Awake()
    {
        inputHub = GetComponent<Controller_PlayerInput>();
        fpsController = GetComponent<Controller_PlayerFPSMovement>();
    }

    public void StartMiniGame(IToolMinigame minigame)
    {
        IMinigameView mini_game_ui = null;
        switch (minigame.GetEquipmentUsed())
        {
            case EquipmentType.NONE:
                {
                    break;
                }
            case EquipmentType.SOCKET_WRENCH:
                {
                    mini_game_ui = wrench_minigame_ui;
                    break;
                }
            default:
                {
                    
                    return;
                }
        }
        if (mini_game_ui == null) 
        {
            TopicLogger.Log(LogTopic.General, LogLevel.CRIT, $"No UI found for minigame using {minigame.GetEquipmentUsed()}");
        }
        Enter(minigame, mini_game_ui);

    }

    public void Enter(IToolMinigame minigame, IMinigameView miniGameUI)
    {
        if (State == PlayerState.WorkingMode)
        {
            Debug.LogError("Cant start mini game. One is already going");
            return;
        }

        State = PlayerState.WorkingMode;
        activeMinigame = minigame;

        inputHub.EnterMiniGame();
        fpsController.OnEnterMiniGame();

        activeMinigame.Begin(OnMinigameComplete, inputHub, miniGameUI);
    }

    void OnMinigameComplete(MinigameResult result)
    {
        activeMinigame.End(result);
        activeMinigame = null;
        State = PlayerState.MovingMode;

        inputHub.ExitMiniGame();
        fpsController.OnExitMiniGame();
    }

    void Update()
    {
        if (State == PlayerState.WorkingMode)
        {
            activeMinigame?.Tick(Time.deltaTime);
        }        
    }

    public Controller_PlayerInput GetUIHub()
    {
        return inputHub;
    }
}



// Placeholder types until the real minigame framework is built.
public struct MinigameResult
{
    public bool Success;
    public float Progress;
}

public interface IToolMinigame
{
    void Begin(Action<MinigameResult> onComplete, Controller_PlayerInput controller, IMinigameView ui);
    void End(MinigameResult result);
    void Tick(float delatTime);
    EquipmentType GetEquipmentUsed();
}
