using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioLibrary library;
    [SerializeField] private AudioSource sourcePrefab;
    [SerializeField] private int poolSize = 16;
    private readonly Queue<AudioSource> m_pool = new();
    private readonly Dictionary<int, AudioSource> m_activeLoops = new();
    private readonly Dictionary<int, Transform> m_loopTargets = new();

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = Instantiate(sourcePrefab, transform);
            src.gameObject.SetActive(false);
            m_pool.Enqueue(src);
        }
    }

    private void OnEnable()
    {
        AudioEvents.OnPlayRequested += Play;
        AudioEvents.OnLoopStartRequested += StartLoop;
        AudioEvents.OnLoopStopRequested += StopLoop;
    }
    private void OnDisable()
    {
        AudioEvents.OnPlayRequested -= Play;
        AudioEvents.OnLoopStartRequested -= StartLoop;
        AudioEvents.OnLoopStopRequested -= StopLoop;
    }

    private void Play(SoundID id, Vector3 position)
    {
        if (!library.TryGet(id, out AudioEventSO evt))
        {
            Debug.LogWarning($"AudioManager: no AudioLibrary entry for {id}");
            return;
        }
        if (evt.clips == null || evt.clips.Length == 0) return;

        AudioSource src = m_pool.Count > 0 ? m_pool.Dequeue() : Instantiate(sourcePrefab, transform);

        src.transform.SetParent(transform, false);
        src.transform.position = position;
        src.clip = evt.GetClip();
        src.volume = evt.GetVolume();
        src.pitch = evt.GetPitch();
        src.outputAudioMixerGroup = evt.mixerGroup;
        src.spatialBlend = evt.spatialBlend;
        src.loop = false;
        src.gameObject.SetActive(true);
        src.Play();

        StartCoroutine(ReturnToPoolAfter(src, src.clip.length));
    }

    private IEnumerator ReturnToPoolAfter(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        src.gameObject.SetActive(false);
        m_pool.Enqueue(src);
    }

    private void StartLoop(SoundID id, Transform followTarget, int handleId)
    {
        if (!library.TryGet(id, out AudioEventSO evt)) return;
        AudioSource src = m_pool.Count > 0 ? m_pool.Dequeue() : Instantiate(sourcePrefab, transform);
        src.clip = evt.GetClip();
        src.volume = evt.GetVolume();
        src.pitch = evt.GetPitch();
        src.outputAudioMixerGroup = evt.mixerGroup;
        src.spatialBlend = evt.spatialBlend;
        src.loop = true;
        // Parent to the target so it follows automatically without per-frame position copying
        src.transform.SetParent(followTarget, false);
        src.transform.localPosition = Vector3.zero;
        src.gameObject.SetActive(true);
        src.Play();
        m_activeLoops[handleId] = src;
        m_loopTargets[handleId] = followTarget;
    }
    private void StopLoop(int handleId)
    {
        if (!m_activeLoops.TryGetValue(handleId, out AudioSource src)) return;
        src.Stop();
        src.transform.SetParent(transform, false); // return to pool root
        src.gameObject.SetActive(false);
        m_pool.Enqueue(src);
        m_activeLoops.Remove(handleId);
        m_loopTargets.Remove(handleId);
    }
    private void Update()
    {
        // Safety net: if a looping sound's target was destroyed without StopLoop being called,
        // release it back to the pool instead of leaking an active AudioSource forever.
        List<int> orphaned = null;
        foreach (var kvp in m_loopTargets)
        {
            if (kvp.Value == null)
            {
                orphaned ??= new List<int>();
                orphaned.Add(kvp.Key);
            }
        }
        if (orphaned != null)
        {
            foreach (int id in orphaned)
                StopLoop(id);
        }
    }
}