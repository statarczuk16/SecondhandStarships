using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MiniGame_Wrench : IToolMinigame
{
    readonly IPartConnector m_part_being_installed;
    Controller_PlayerInput m_input_hub;
    MiniGame_Wrench_UI_Script m_ui_script;
    int progress_per_action = 16;

    float wrenchPosition; // Managed by held-button cursor movement
    float loosenSweetSpotCenter;
    float tightenSweetSpotCenter;
    float sweetSpotHeight = 0.12f; //as vertical percentage of the bar
    float cursorSpeed = 0.6f; // units of [0,1] bar per second

    bool isPrimaryHeld;
    bool isSecondaryHeld;
    private HashSet<InstallationState> m_goal;


    private Action m_on_always;
    private Action m_on_success;
    private Action m_on_failure;
    EquipmentType m_tool;
    private InputMode m_game_end_input_mode;
    private Action m_mini_game_cleanup_func;

    public MiniGame_Wrench(IPartConnector mPartBeingInstalled, EquipmentType mTool,  HashSet<InstallationState> goal)
    {
        this.m_part_being_installed = mPartBeingInstalled;
        this.m_tool = mTool;
        this.m_goal = new HashSet<InstallationState>(goal);
    }

    public EquipmentType GetEquipmentUsed()
    {
        return this.m_tool;
    }

    
    public void Begin(Controller_PlayerInput controller, IMinigameView ui, Action mini_game_cleanup_func)
    {
        
        wrenchPosition = 0.5f; // Start centered
        isPrimaryHeld = false;
        isSecondaryHeld = false;
        this.m_input_hub = controller;
        PickNewSweetSpot();
        
        

        // Track held state for both buttons independently
        m_input_hub.Controls.MenuMode.PrimaryButton.started += OnPrimaryPressed;
        m_input_hub.Controls.MenuMode.PrimaryButton.canceled += OnPrimaryReleased;
        m_input_hub.Controls.MenuMode.SecondaryButton.started += OnSecondaryPressed;
        m_input_hub.Controls.MenuMode.SecondaryButton.canceled += OnSecondaryReleased;
        m_input_hub.Controls.MenuMode.Cancel.performed += OnPlayerCanceledGame;
        this.m_ui_script = (MiniGame_Wrench_UI_Script)ui;
        m_ui_script.Show();
        m_ui_script.SetWrenchPosition(wrenchPosition);
        m_ui_script.UpdateTaskProgress(0f);
        this.m_mini_game_cleanup_func = mini_game_cleanup_func;
    }
    
    public void SetOutcomes(InputMode game_end_input_mode,Action on_always, Action on_success, Action on_failure)
    {
        this.m_game_end_input_mode = game_end_input_mode;
        this.m_on_always = on_always;
        this.m_on_failure = on_failure;
        this.m_on_success = on_success;
        
    }



    private void OnPlayerCanceledGame(InputAction.CallbackContext context)
    {
        End(MiniGameResult.NEUTRAL);
    }

    public void Tick(float deltaTime)
    {
        // Move cursor only when exactly one button is held; both-or-neither = no movement
        if (isPrimaryHeld && !isSecondaryHeld)
        {
            wrenchPosition = Mathf.Clamp01(wrenchPosition + cursorSpeed * deltaTime);
        }
        else if (isSecondaryHeld && !isPrimaryHeld)
        {
            wrenchPosition = Mathf.Clamp01(wrenchPosition - cursorSpeed * deltaTime);
        }

        m_ui_script.SetTightenSweetSpot(tightenSweetSpotCenter, sweetSpotHeight);
        m_ui_script.SetLoosenSweetSpot(loosenSweetSpotCenter, sweetSpotHeight);
        m_ui_script.UpdateTaskProgress(this.m_part_being_installed.GetInstallationProgress());
        m_ui_script.SetWrenchPosition(wrenchPosition);
    }

    private void OnPrimaryPressed(InputAction.CallbackContext ctx)
    {
        isPrimaryHeld = true;
    }

    private void OnPrimaryReleased(InputAction.CallbackContext ctx)
    {
        isPrimaryHeld = false;
        TryEvaluateRelease();
    }

    private void OnSecondaryPressed(InputAction.CallbackContext ctx)
    {
        isSecondaryHeld = true;
    }

    private void OnSecondaryReleased(InputAction.CallbackContext ctx)
    {
        isSecondaryHeld = false;
        TryEvaluateRelease();
    }

    // Only evaluate once both buttons are fully released (handles the case
    // where a player lets go of one button while still holding the other)
    private void TryEvaluateRelease()
    {
        if (isPrimaryHeld || isSecondaryHeld)
        {
            return;
        }

        // Distance from cursor to CENTER of sweet spot vs half the sweet spot HEIGHT
        bool hit_loosen_sweet_spot = Mathf.Abs(wrenchPosition - loosenSweetSpotCenter) <= sweetSpotHeight * 0.5f;
        bool hit_tighten_sweet_spot = Mathf.Abs(wrenchPosition - tightenSweetSpotCenter) <= sweetSpotHeight * 0.5f;
        bool hit_a_sweet_spot = hit_tighten_sweet_spot || hit_loosen_sweet_spot;

        if (hit_a_sweet_spot)
        {
            
            m_ui_script.FlashHit();
            AudioEvents.Fire(SoundID.Bolt_Hit, this.m_input_hub.transform.position); //inputhub is on the player
            if (wrenchPosition < 0.5f)
            {
                // Bottom half -> Tighten (adds to bolt progress)
                m_part_being_installed.InstallationUpdate(progress_per_action);
            }
            else
            {
                // Top half -> Loosen (subtracts progress)
                m_part_being_installed.InstallationUpdate(-progress_per_action);
            }

            //We are done when a bolt that started uninstalled becomes secure, or when a bolt that started secure becomes uninstalled.
            if (m_goal.Contains(m_part_being_installed.GetInstallState()))
            {
                End(MiniGameResult.SUCCESS);
                return;
            }

            // Reset positioning and find a new target area for the next attempt
            wrenchPosition = 0.5f;
            PickNewSweetSpot();
        }
        else
        {
            AudioEvents.Fire(SoundID.Bolt_Miss, this.m_input_hub.transform.position); //inputhub is on the player
            m_ui_script.FlashMiss();
        }

        wrenchPosition = 0.5f;
    }

    void PickNewSweetSpot()
    {
        //pick sweet spot for loose
        loosenSweetSpotCenter = UnityEngine.Random.Range(0.75f, 0.9f);  // Loosen Zone
        //
        tightenSweetSpotCenter = 1f - loosenSweetSpotCenter; //Tighten zone - mirror of loosen sweet spot position
    }




    public void End(MiniGameResult result)
    {
        // Clean up action listeners completely
        m_input_hub.Controls.MenuMode.PrimaryButton.started -= OnPrimaryPressed;
        m_input_hub.Controls.MenuMode.PrimaryButton.canceled -= OnPrimaryReleased;
        m_input_hub.Controls.MenuMode.SecondaryButton.started -= OnSecondaryPressed;
        m_input_hub.Controls.MenuMode.SecondaryButton.canceled -= OnSecondaryReleased;
        m_input_hub.Controls.MenuMode.Cancel.performed -= OnPlayerCanceledGame;

        m_ui_script.Hide();

        if (result == MiniGameResult.SUCCESS)
        {
            m_on_success?.Invoke();
        }
        else if(result == MiniGameResult.FAILURE)
        {
            m_on_failure?.Invoke();
        }
        m_on_always?.Invoke();
        m_mini_game_cleanup_func?.Invoke();
       
    }

    public InputMode GetInputModeDesiredAfterFinish()
    {
        return this.m_game_end_input_mode;
    }

   
}