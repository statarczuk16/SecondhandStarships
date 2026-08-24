public interface IToggleable
{
    bool CanToggle(out string reason);
    void ToggleWantsToBeOn();
    bool IsOn(); //IE light is on or off
    bool WantsToBeOn();//IE light switch is on or off

    void TurnOff();

    void TurnOn();
    bool OnRequirementsMet(out string reason);//IE power to the light. Wants to be on + requirements met = is on
}


