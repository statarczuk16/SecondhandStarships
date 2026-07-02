using UnityEngine;
using UnityEngine.UIElements;


public interface IMinigameView
{
    void Show();
    void Hide();
    void UpdateTaskProgress(float progress);
    void FlashHit();
    void FlashMiss();
}


public class MiniGame_Wrench_UI_Script : MonoBehaviour, IMinigameView
{
    private VisualElement rootContainer;
    private VisualElement CalibrationTrack;
    private VisualElement SweetZoneElementLoose;
    private VisualElement SweetZoneElementTight;
    private VisualElement FrequencyIndicator;
    private Label StatusMessageLabel;

    // HUD Amber Color Palette Configs
    private readonly Color hudAmberActive = new Color(1.0f, 0.62f, 0.0f, 0.85f);
    private readonly Color hudGreenLock = new Color(0.0f, 0.95f, 0.4f, 0.9f);
    private readonly Color hudRedWarning = new Color(1.0f, 0.15f, 0.15f, 0.9f);

    private void OnEnable()
    {
        var panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
        {
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnDisable()
    {
        var panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnUIReload(PanelRenderer pr, VisualElement root)
    {
        rootContainer = root;

        CalibrationTrack = rootContainer.QOrFail<VisualElement>("CalibrationTrack");
        SweetZoneElementLoose = rootContainer.QOrFail<VisualElement>("SweetZoneElementLoose");
        SweetZoneElementTight = rootContainer.QOrFail<VisualElement>("SweetZoneElementTight");
        FrequencyIndicator = rootContainer.QOrFail<VisualElement>("FrequencyIndicator");
        StatusMessageLabel = rootContainer.QOrFail<Label>("StatusMessageLabel");

        // Ensure the UI starts hidden once it finishes loading
        Hide();
    }

    public void Show()
    {
        if (rootContainer != null)
        {
            rootContainer.style.display = DisplayStyle.Flex;

            if (StatusMessageLabel != null)
            {
                StatusMessageLabel.text = "//BOLT_TIGHTNESS - 0";
                StatusMessageLabel.style.color = hudAmberActive;
            }
        }
    }

    public void Hide()
    {
        if (rootContainer != null)
        {
            rootContainer.style.display = DisplayStyle.None;
        }
    }

    public void SetWrenchPosition(float positionNormalized)
    {
        float indicatorTopPositionPercent = (1f - positionNormalized) * 100f;
        FrequencyIndicator.style.top = Length.Percent(indicatorTopPositionPercent);
        // Reset elements back to default HUD Amber standard lines during dragging transitions
        FrequencyIndicator.style.backgroundColor = hudAmberActive;
    }

    public void SetTightenSweetSpot(float sweetSpotCenterNormalized, float sweetSpotWidthNormalized)
    {
       

        // --- Vertical Math Layout Translation ---
        // UI Toolkit positions 0% at the TOP and 100% at the BOTTOM.
        // 0f = Down/Tighten (Bottom of UI), 1f = Up/Loosen (Top of UI). Invert to map correctly.
        float sweetZoneHeightPercent = sweetSpotWidthNormalized * 100f;
        float sweetZoneTopPositionPercent = (1f - (sweetSpotCenterNormalized + (sweetSpotWidthNormalized * 0.5f))) * 100f;

        SweetZoneElementTight.style.height = Length.Percent(sweetZoneHeightPercent);
        SweetZoneElementTight.style.top = Length.Percent(sweetZoneTopPositionPercent);

        StatusMessageLabel.style.color = hudAmberActive;

    }

    public void SetLoosenSweetSpot(float sweetSpotCenterNormalized, float sweetSpotWidthNormalized)
    {
        

        // --- Vertical Math Layout Translation ---
        // UI Toolkit positions 0% at the TOP and 100% at the BOTTOM.
        // 0f = Down/Tighten (Bottom of UI), 1f = Up/Loosen (Top of UI). Invert to map correctly.
        float sweetZoneHeightPercent = sweetSpotWidthNormalized * 100f;
        float sweetZoneTopPositionPercent = (1f - (sweetSpotCenterNormalized + (sweetSpotWidthNormalized * 0.5f))) * 100f;

        SweetZoneElementLoose.style.height = Length.Percent(sweetZoneHeightPercent);
        SweetZoneElementLoose.style.top = Length.Percent(sweetZoneTopPositionPercent);
       
        StatusMessageLabel.style.color = hudAmberActive;
        
    }

    public void UpdateTaskProgress(float progress)
    {
        
        StatusMessageLabel.text = $"// BOLT_TIGHTNESS - {progress:F1}";
        
    }

    public void FlashHit()
    {
        
        StatusMessageLabel.style.color = hudGreenLock;
        FrequencyIndicator.style.backgroundColor = hudGreenLock;
        
    }

    public void FlashMiss()
    {
        if (StatusMessageLabel != null)
        {
            StatusMessageLabel.style.color = hudRedWarning;
        }

        if (FrequencyIndicator != null)
        {
            FrequencyIndicator.style.backgroundColor = hudRedWarning;
        }
    }
}