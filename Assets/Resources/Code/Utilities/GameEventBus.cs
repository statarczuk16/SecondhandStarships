using System;

public static class GameEventBus
{
    public static event Action<InputMode> RequestChangeInputMode;


    public static void FireInputModeEvent(InputMode mode)
    {
        RequestChangeInputMode?.Invoke(mode);
    }
}