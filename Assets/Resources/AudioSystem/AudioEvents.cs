using System;
using UnityEngine;

public static class AudioEvents
{
    public static event Action<SoundID, Vector3> OnPlayRequested;
    public static event Action<SoundID, Transform, int> OnLoopStartRequested;
    public static event Action<int> OnLoopStopRequested;

    private static int s_nextHandleId = 0;

    public static void Fire(SoundID id, Vector3 position = default)
    {
        if (id == SoundID.None) return;
        OnPlayRequested?.Invoke(id, position);
    }

    public static AudioHandle StartLoop(SoundID id, Transform followTarget)
    {
        if (id == SoundID.None) return AudioHandle.Invalid;
        int handleId = s_nextHandleId++;
        OnLoopStartRequested?.Invoke(id, followTarget, handleId);
        return new AudioHandle(handleId);
    }

    public static void StopLoop(AudioHandle handle)
    {
        if (!handle.IsValid) return;
        OnLoopStopRequested?.Invoke(handle.id);
    }
}