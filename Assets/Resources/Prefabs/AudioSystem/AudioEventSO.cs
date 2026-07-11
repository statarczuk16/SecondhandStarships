using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Audio Event")]
public class AudioEventSO : ScriptableObject
{
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 1f)] public float volumeVariance = 0f;
    public float pitch = 1f;
    [Range(0f, 1f)] public float pitchVariance = 0f;
    public AudioMixerGroup mixerGroup;
    public bool loop = false;
    [Range(0f, 1f)] public float spatialBlend = 1f; // 0 = 2D (UI), 1 = 3D (world)

    public AudioClip GetClip() => clips[Random.Range(0, clips.Length)];
    public float GetVolume() => volume + Random.Range(-volumeVariance, volumeVariance);
    public float GetPitch() => pitch + Random.Range(-pitchVariance, pitchVariance);
}