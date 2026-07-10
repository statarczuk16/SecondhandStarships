using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;

public class MiniGame_Wrench : IToolMinigame
{
    readonly Component_BoltSlot bolt;
    Controller_PlayerInput inputHub;
    MiniGame_Wrench_UI_Script ui_script;
    int progress_per_action = 16;

    float wrenchPosition; // Managed entirely by player's physical dragging movement
    float loosenSweetSpotCenter;
    float tightenSweetSpotCenter;
    bool isDragging;
    float sweetSpotHeight = 0.12f; //as vertical percentage of the bar
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
        isDragging = false;
        this.inputHub = inputHub;
        PickNewSweetSpot();
        this.ui_script = (MiniGame_Wrench_UI_Script)ui;

        // 1. Bind 'Pull' (Hold interaction) to detect starting and ending a drag action
        inputHub.Controls.WorkingMode.PrimaryButtonHoldAndDrag.started += OnDragStarted;
        inputHub.Controls.WorkingMode.PrimaryButtonHoldAndDrag.canceled += OnDragReleased;
        inputHub.Controls.WorkingMode.Cancel.performed += OnPlayerCanceledGame;

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
        if (isDragging)
        {
            // 2. Read the delta change of the mouse/pointer or stick movement
            Vector2 dragDelta = inputHub.Controls.WorkingMode.DragDelta.ReadValue<Vector2>();

            // Adjust this modifier to control sensitivity (how far they must physically drag)
            float dragSensitivity = 0.003f;
            float verticalMovement = dragDelta.y * dragSensitivity;

            // Apply movement and clamp inside the bar bounds [0, 1]
            wrenchPosition = Mathf.Clamp01(wrenchPosition + verticalMovement);
            Debug.Log(wrenchPosition);

            
        }
        
        ui_script.SetTightenSweetSpot(tightenSweetSpotCenter, sweetSpotHeight);
        ui_script.SetLoosenSweetSpot(loosenSweetSpotCenter, sweetSpotHeight);
        ui_script.UpdateTaskProgress(this.bolt.GetInstallationProgress());
        ui_script.SetWrenchPosition(wrenchPosition);
        
    }

    private void OnDragStarted(InputAction.CallbackContext ctx)
    {
        isDragging = true;
    }

    private void OnDragReleased(InputAction.CallbackContext ctx)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

       //if dist between the wrench and the CENTER of sweet spot is less than half the HEIGHT of the sweet spot, then we hit inside of the sweet spot
        bool hit_loosen_sweet_spot = Mathf.Abs(wrenchPosition - loosenSweetSpotCenter) <= sweetSpotHeight * 0.5f;
        bool hit_tighten_sweet_spot = Mathf.Abs(wrenchPosition - tightenSweetSpotCenter) <= sweetSpotHeight * 0.5f;
        bool hit_a_sweet_spot = hit_tighten_sweet_spot || hit_loosen_sweet_spot;
        if (hit_a_sweet_spot)
        {
            ui_script.FlashHit();

            // Check if we dragged DOWN (Tighten) or UP (Loosen) relative to the middle
            if (wrenchPosition < 0.5f)
            {
                // Dragged down -> Tighten (adds to bolt progress)
                bolt.InstallationUpdate( progress_per_action);
            }
            else
            {
                // Dragged up -> Loosen (subtracts progress by turning input negative)
                bolt.InstallationUpdate( -progress_per_action);
            }


            //We are done when a bolt that started uninstalled becomes secure, or when a bolt that started secure becomes uninstalled.
            if(bolt.GetInstallState() == InstallationState.UNINSTALLED || bolt.GetInstallState() == InstallationState.INSTALLED)
            {
                Finish(true);
            }


            // Reset positioning and find a new target area for the next pull
            wrenchPosition = 0.5f;
            PickNewSweetSpot();
        }
        else
        {
            
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
        inputHub.Controls.WorkingMode.PrimaryButtonHoldAndDrag.started -= OnDragStarted;
        inputHub.Controls.WorkingMode.PrimaryButtonHoldAndDrag.canceled -= OnDragReleased;
        inputHub.Controls.WorkingMode.Cancel.performed -= OnPlayerCanceledGame;

        ui_script.Hide();

        onComplete?.Invoke(new MinigameResult
        {
            Success = success,
        });
    }

    public void End(MinigameResult result) { /* VFX/SFX hook on completion, if any */ }
}