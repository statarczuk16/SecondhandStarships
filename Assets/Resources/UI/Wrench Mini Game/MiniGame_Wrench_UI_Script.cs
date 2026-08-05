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
                ResetThemeState();
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
        
        // Reset indicator and label colors back to primary active state during movement
        ResetThemeState();
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
    }

    public void UpdateTaskProgress(float progress)
    {
        if (StatusMessageLabel != null)
        {
            StatusMessageLabel.text = $"// BOLT_TIGHTNESS - {progress:F1}";
        }
    }

    public void FlashHit()
    {
        if (StatusMessageLabel != null)
        {
            StatusMessageLabel.RemoveFromClassList("theme-text-primary");
            StatusMessageLabel.RemoveFromClassList("theme-text-danger");
            StatusMessageLabel.AddToClassList("theme-text-success");
        }

        if (FrequencyIndicator != null)
        {
            FrequencyIndicator.RemoveFromClassList("theme-border-active");
            FrequencyIndicator.RemoveFromClassList("theme-border-danger");
            FrequencyIndicator.AddToClassList("theme-border-selected");
        }
    }

    public void FlashMiss()
    {
        if (StatusMessageLabel != null)
        {
            StatusMessageLabel.RemoveFromClassList("theme-text-primary");
            StatusMessageLabel.RemoveFromClassList("theme-text-success");
            StatusMessageLabel.AddToClassList("theme-text-danger");
        }

        if (FrequencyIndicator != null)
        {
            FrequencyIndicator.RemoveFromClassList("theme-border-active");
            FrequencyIndicator.RemoveFromClassList("theme-border-selected");
            FrequencyIndicator.AddToClassList("theme-border-danger");
        }
    }

    private void ResetThemeState()
    {
        if (StatusMessageLabel != null)
        {
            StatusMessageLabel.RemoveFromClassList("theme-text-success");
            StatusMessageLabel.RemoveFromClassList("theme-text-danger");
            StatusMessageLabel.AddToClassList("theme-text-primary");
        }

        if (FrequencyIndicator != null)
        {
            FrequencyIndicator.RemoveFromClassList("theme-border-selected");
            FrequencyIndicator.RemoveFromClassList("theme-border-danger");
            FrequencyIndicator.AddToClassList("theme-border-active");
        }
    }
}