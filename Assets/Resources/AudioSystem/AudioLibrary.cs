using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundID
{
    None = 0,

    Bolt_Hit,
    Bolt_Miss,
    Bolt_Complete,

    Part_Placed,
    Part_Installed,
    Part_Uninstalled,

    UI_ButtonClick,
    UI_PanelOpen,
    UI_PanelClose,
    
    Generator,
    
    Toggle,
    DoorOpen,
    DoorClose,
    Light_Butane_Torch
}

[CreateAssetMenu(menuName = "Audio/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [Serializable]
    private struct Entry
    {
        public SoundID id;
        public AudioEventSO audioEvent;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<SoundID, AudioEventSO> m_lookup;

    private void OnEnable()
    {
        m_lookup = new Dictionary<SoundID, AudioEventSO>();
        foreach (var entry in entries)
        {
            if (entry.audioEvent == null) continue;

            if (!m_lookup.TryAdd(entry.id, entry.audioEvent))
                Debug.LogWarning($"AudioLibrary: duplicate entry for {entry.id}, ignoring second.");
        }
    }

    public bool TryGet(SoundID id, out AudioEventSO audioEvent)
    {
        return m_lookup.TryGetValue(id, out audioEvent);
    }
}