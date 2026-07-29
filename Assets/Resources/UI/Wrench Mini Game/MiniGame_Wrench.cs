using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MiniGame_Wrench : IToolMinigame
{
    readonly Component_BoltSlot bolt;
    Controller_PlayerInput inputHub;
    MiniGame_Wrench_UI_Script ui_script;
    int progress_per_action = 16;

    float wrenchPosition; // Managed by held-button cursor movement
    float loosenSweetSpotCenter;
    float tightenSweetSpotCenter;
    float sweetSpotHeight = 0.12f; //as vertical percentage of the bar
    float cursorSpeed = 0.6f; // units of [0,1] bar per second

    bool isPrimaryHeld;
    bool isSecondaryHeld;

    InstallationState goal;
    Action<MinigameResult> onComplete;
    EquipmentType tool;

    public MiniGame_Wrench(Component_BoltSlot bolt, EquipmentType tool)
    {
        this.bolt = bolt;
        this.tool = tool;
    }

    public EquipmentType GetEquipmentUsed()
    {
        return this.tool;
    }

    public void Begin(Action<MinigameResult> onComplete, Controller_PlayerInput inputHub, IMinigameView ui)
    {
        this.onComplete = onComplete;
        wrenchPosition = 0.5f; // Start centered
        isPrimaryHeld = false;
        isSecondaryHeld = false;
        this.inputHub = inputHub;
        PickNewSweetSpot();
        this.ui_script = (MiniGame_Wrench_UI_Script)ui;

        // Track held state for both buttons independently
        inputHub.Controls.MenuMode.PrimaryButton.started += OnPrimaryPressed;
        inputHub.Controls.MenuMode.PrimaryButton.canceled += OnPrimaryReleased;
        inputHub.Controls.MenuMode.SecondaryButton.started += OnSecondaryPressed;
        inputHub.Controls.MenuMode.SecondaryButton.canceled += OnSecondaryReleased;
        inputHub.Controls.MenuMode.Cancel.performed += OnPlayerCanceledGame;

        ui_script.Show();
        ui_script.SetWrenchPosition(wrenchPosition);
        ui_script.UpdateTaskProgress(0f);
    }

    private void OnPlayerCanceledGame(InputAction.CallbackContext context)
    {
        Finish(false);
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

        ui_script.SetTightenSweetSpot(tightenSweetSpotCenter, sweetSpotHeight);
        ui_script.SetLoosenSweetSpot(loosenSweetSpotCenter, sweetSpotHeight);
        ui_script.UpdateTaskProgress(this.bolt.GetInstallationProgress());
        ui_script.SetWrenchPosition(wrenchPosition);
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
            
            ui_script.FlashHit();
            AudioEvents.Fire(SoundID.Bolt_Hit, this.bolt.transform.position);
            if (wrenchPosition < 0.5f)
            {
                // Bottom half -> Tighten (adds to bolt progress)
                bolt.InstallationUpdate(progress_per_action);
            }
            else
            {
                // Top half -> Loosen (subtracts progress)
                bolt.InstallationUpdate(-progress_per_action);
            }

            //We are done when a bolt that started uninstalled becomes secure, or when a bolt that started secure becomes uninstalled.
            if (bolt.GetInstallState() == InstallationState.UNINSTALLED || bolt.GetInstallState() == InstallationState.INSTALLED)
            {
                Finish(true);
                return;
            }

            // Reset positioning and find a new target area for the next attempt
            wrenchPosition = 0.5f;
            PickNewSweetSpot();
        }
        else
        {
            AudioEvents.Fire(SoundID.Bolt_Miss, this.bolt.transform.position);
            ui_script.FlashMiss();
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

    void Finish(bool success)
    {
        // Clean up action listeners completely
        inputHub.Controls.MenuMode.PrimaryButton.started -= OnPrimaryPressed;
        inputHub.Controls.MenuMode.PrimaryButton.canceled -= OnPrimaryReleased;
        inputHub.Controls.MenuMode.SecondaryButton.started -= OnSecondaryPressed;
        inputHub.Controls.MenuMode.SecondaryButton.canceled -= OnSecondaryReleased;
        inputHub.Controls.MenuMode.Cancel.performed -= OnPlayerCanceledGame;

        ui_script.Hide();

        onComplete?.Invoke(new MinigameResult
        {
            Success = success,
        });
    }

    public void End(MinigameResult result) { /* VFX/SFX hook on completion, if any */ }
}