using System;
using UnityEngine;

/// <summary>
/// Marks a snap point that Editor_PrefabMountPlacer (and anything else that wants to)
/// can connect prefabs together at. Local +Z (transform.forward) is this mount's
/// outward-facing normal - two connected mount points end up face-to-face, normals
/// opposed.
/// </summary>
public class Component_MountPoint : MonoBehaviour
{
    public enum MountRole
    {
        Any,    // Will pair with anything.
        Socket, // Only pairs with a Plug (or Any).
        Plug    // Only pairs with a Socket (or Any).
    }

    [Tooltip("Stable identity for this mount point, auto-generated once. Lets code match 'the same' mount point across separate instances of this prefab (e.g. a preview ghost vs. the real placed object) without relying on component ordering.")]
    [SerializeField] private string m_localId = "";

    [Tooltip("Free-form tag restricting what this can connect to (e.g. \"Pipe\", \"WallPanel\"). Empty matches any tag.")]
    [SerializeField] private string m_mountTag = "";

    [Tooltip("Socket/Plug pairing. Any matches everything. Socket only pairs with Plug and vice versa, so you can't snap two sockets together.")]
    [SerializeField] private MountRole m_role = MountRole.Any;

    [Tooltip("Per-mount override for the placement tool's search radius. Leave <= 0 to use the tool's default radius.")]
    [SerializeField] private float m_snapRadiusOverride = -1f;

    // Serialized so a connection made in the editor survives script recompiles,
    // scene saves/reloads, and is available at runtime too - not just placer bookkeeping.
    [SerializeField] private Component_MountPoint m_connectedTo;
    public Component_MountPoint snap_candidate;
    public float snap_candidate_dist;

    public string MountTag => m_mountTag;
    public MountRole Role => m_role;
    public float SnapRadiusOverride => m_snapRadiusOverride;
    public bool IsOccupied => m_connectedTo != null;
    public Component_MountPoint ConnectedTo => m_connectedTo;

    public string GetLocalId()
    {
        if (!HasLocalId)
        {
            throw new Exception("Mount Missing local ID " + this.name + " " + this.transform.parent.name);
        }
        return this.m_localId;
    }
    public bool HasLocalId => !string.IsNullOrEmpty(m_localId) && m_localId != "-1";

    /// <summary>
    /// Assigns a new ID only if one isn't already set. Intended to be called by
    /// editor tooling directly on a prefab *asset* (see Editor_PrefabMountPlacer's
    /// EnsureMountPointIdsAssigned) so every instance made from that asset - a
    /// preview ghost, a placed copy, whatever - inherits the same, already-set ID.
    /// </summary>
    public void AssignIdIfMissing()
    {
        if (string.IsNullOrEmpty(m_localId) || m_localId == "-1")
        {
            m_localId = System.Guid.NewGuid().ToString("N");
        }
    }

    // Unity calls Reset() when this component is first added in the Editor
    // (and via the inspector's "Reset" context menu). Generating the ID here
    // means most mount points get a unique, stable ID with zero manual setup.
    // Anything added another way (e.g. via script) is caught by
    // Editor_PrefabMountPlacer.EnsureMountPointIdsAssigned instead.
    private void Reset()
    {
        AssignIdIfMissing();
    }

    public Transform GetMountPoint()
    {
        return transform;
    }

    /// <summary>
    /// Whether this mount point is allowed to connect to other: roles must be
    /// compatible (Socket only with Plug, unless either side is Any) and tags
    /// must match (an empty tag on either side matches anything).
    /// </summary>
    public bool CanConnectTo(Component_MountPoint other)
    {
        if (other == null || other == this || other.IsOccupied) return false;

        bool roleCompatible =
            m_role == MountRole.Any || other.m_role == MountRole.Any ||
            (m_role == MountRole.Socket && other.m_role == MountRole.Plug) ||
            (m_role == MountRole.Plug && other.m_role == MountRole.Socket);

        if (!roleCompatible) return false;

        return string.IsNullOrEmpty(m_mountTag) || string.IsNullOrEmpty(other.m_mountTag) || m_mountTag == other.m_mountTag;
    }
    
    public static void ConnectMounts(Component_MountPoint one, Component_MountPoint two)
    {
        Debug.Log($"ConnectMounts {one.transform.parent.name}:{one.name} <-> {two.transform.parent.name}:{two.name}");

    #if UNITY_EDITOR
            // Tell Unity we are about to change these objects so it can track the change
            if (!Application.isPlaying)
            {
                UnityEditor.Undo.RecordObject(one, "Connect Mounts");
                UnityEditor.Undo.RecordObject(two, "Connect Mounts");
            }
    #endif

            one.m_connectedTo = two;
            two.m_connectedTo = one;

    #if UNITY_EDITOR
            // Tell Unity the objects have been modified and need to be saved
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(one);
                UnityEditor.EditorUtility.SetDirty(two);
                
                // If these mounts belong to prefab instances in the scene, 
                // this ensures the override is recorded properly!
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(one);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(two);
            }
    #endif
    }

    public void ClearConnection()
    {
        TopicLogger.Log(LogTopic.General, LogLevel.WARN, $"Mount connection cleared");
        m_connectedTo = null;
    }
    
    private void OnDestroy()
    {
        if (m_connectedTo != null)
        {
            m_connectedTo.ClearConnection();
           ClearConnection();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        
        if (snap_candidate != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, snap_candidate.GetMountPoint().position);
            string label = $"{snap_candidate_dist}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.05f, label);
        }
        else
        {
            Gizmos.color = IsOccupied ? new Color(1f, 0.4f, 0.4f) : new Color(0.3f, 1f, 1f);
            Gizmos.DrawSphere(transform.position, 0.03f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.25f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        string label = string.IsNullOrEmpty(m_mountTag) ? m_role.ToString() : $"{m_role} [{m_mountTag}]";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.05f, label);
        if (m_connectedTo != null)
        {
            Gizmos.DrawLine(transform.position, this.m_connectedTo.GetMountPoint().position);
        }
       
    }
#endif
}