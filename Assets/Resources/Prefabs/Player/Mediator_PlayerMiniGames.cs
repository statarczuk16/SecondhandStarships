using System;
using UnityEngine;

public enum InputMode { MovingMode, MenuMode }

public enum MiniGameResult
{
    SUCCESS,
    FAILURE,
    NEUTRAL
}

/// <summary>
/// Conductor for the player's input state. Owns the transition between free-roam
/// and tool minigames, and pokes every dependent system so they change together
/// instead of drifting out of sync.
/// </summary>
[RequireComponent(typeof(Controller_PlayerInput))]
[RequireComponent(typeof(Controller_PlayerFPSMovement))]
public class Mediator_PlayerMiniGames : MonoBehaviour
{
    public InputMode State { get; private set; } = InputMode.MovingMode;

    Controller_PlayerInput m_input_hub;
    Controller_PlayerFPSMovement m_fps_controller;
    [SerializeField] MiniGame_Wrench_UI_Script m_wrench_minigame_ui;
    IToolMinigame m_active_minigame;

    void Awake()
    {
        m_input_hub = GetComponent<Controller_PlayerInput>();
        m_fps_controller = GetComponent<Controller_PlayerFPSMovement>();
        if (!m_wrench_minigame_ui)
        {
            throw new Exception("Wrench Mini Game not set!");
        }
    }
    
    void OnEnable()
    {
        // LISTEN: Subscribe to the global event bus
        GameEventBus.RequestChangeInputMode += ChangeInputMode;
    }

    void OnDisable()
    {
        // CLEANUP: Always unsubscribe to prevent memory leaks
        GameEventBus.RequestChangeInputMode -= ChangeInputMode;
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
            case EquipmentType.SCREW_DRIVER:
                {
                    mini_game_ui = m_wrench_minigame_ui;
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
        if (m_active_minigame != null)
        {
            Debug.LogError("Cant start mini game. One is already going");
            return;
        }
       ChangeInputMode(InputMode.MenuMode);
       m_active_minigame = minigame;
       InputMode targetMode = m_active_minigame.GetInputModeDesiredAfterFinish();
       m_active_minigame.Begin(m_input_hub, miniGameUI, () => MiniGameCleanupFunc(targetMode));
    }

    public void MiniGameCleanupFunc(InputMode new_mode)
    {
        ChangeInputMode(new_mode);
        m_active_minigame = null;
    }

    public void ChangeInputMode(InputMode new_state)
    {
        State = new_state;

        switch (new_state)
        {
            case InputMode.MenuMode:
            {
                m_input_hub.EnterMiniGame();
                m_fps_controller.OnEnterMiniGame();
                break;
            }
            case InputMode.MovingMode:
            {
                m_input_hub.ExitMiniGame();
                m_fps_controller.OnExitMiniGame();
                break;
            }
            default:
            {
                TopicLogger.Log(LogTopic.Interaction, LogLevel.CRIT, $"Illegal state {new_state}");
                break;
            }
        }
    }

    void Update()
    {
        if (State == InputMode.MenuMode)
        {
            m_active_minigame?.Tick(Time.deltaTime);
        }        
    }

    public Controller_PlayerInput GetUIHub()
    {
        return m_input_hub;
    }
}



public interface IToolMinigame
{
    void SetOutcomes(InputMode game_end_input_mode, Action on_always, Action on_success, Action on_failure);
    void Begin(Controller_PlayerInput controller, IMinigameView ui, Action mini_game_cleanup_func);
    void End(MiniGameResult result);
    void Tick(float delatTime);
    EquipmentType GetEquipmentUsed();

    InputMode GetInputModeDesiredAfterFinish();
}
