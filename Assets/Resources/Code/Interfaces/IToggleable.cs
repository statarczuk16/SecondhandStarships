public interface IToggleable
{
    bool CanToggle(out string reason);

    void Toggle();
    bool IsOn();

}
