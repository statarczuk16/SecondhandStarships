using System.Collections.Generic;
using UnityEngine;

public static class GhostPreviewFactory
{
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    // Only one build-ghost is ever active at a time, so blocker tint state
    // is global rather than per-surface.
    private static readonly Dictionary<Renderer, MaterialPropertyBlock> s_tintedRenderers = new Dictionary<Renderer, MaterialPropertyBlock>();

    /// <summary>
    /// Creates a visual-only ghost by instantiating the real prefab under parent
    /// (mirroring the real spawn logic) and stripping all non-render components.
    /// </summary>
    public static GameObject CreateGhost(GameObject source, Transform parent, Color? tint = null)
    {
        GameObject ghost = Object.Instantiate(source, parent, false);
        ghost.name = source.name + "_Ghost";

        ghost.transform.localPosition = Vector3.zero;

        StripToVisualOnly(ghost);

        if (tint.HasValue)
        {
            ApplyTint(ghost, tint.Value);
        }

        return ghost;
    }

    private static void StripToVisualOnly(GameObject root)
    {
        foreach (Component c in root.GetComponentsInChildren<Component>(true))
        {
            if (c is Transform || c is MeshFilter || c is MeshRenderer || c is SkinnedMeshRenderer || c is Component_PrefabBoundary)
                continue;

            Object.Destroy(c);
        }
    }

    public static void ApplyTint(GameObject ghost, Color color)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor(EmissionColor, color);

        foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>(true))
        {
            r.SetPropertyBlock(block);
        }
    }

    public static void Destroy(GameObject ghost)
    {
        if (ghost != null)
            Object.Destroy(ghost);
    }

    /// <summary>
    /// Tints the renderers of every GameObject in blockers red, preserving each
    /// renderer's existing MaterialPropertyBlock so only color is overwritten.
    /// Anything previously tinted but no longer in blockers is restored.
    /// Call ClearBlockerTints() when the build ghost itself is cleared.
    /// </summary>
    public static void UpdateBlockerTints(HashSet<GameObject> blockers)
    {
        HashSet<Renderer> stillBlocking = new HashSet<Renderer>();
        foreach (GameObject blocker in blockers)
        {
            foreach (Renderer r in blocker.GetComponentsInChildren<Renderer>())
            {
                stillBlocking.Add(r);
            }
        }

        List<Renderer> toRestore = new List<Renderer>();
        foreach (var kvp in s_tintedRenderers)
        {
            if (kvp.Key == null || !stillBlocking.Contains(kvp.Key))
            {
                toRestore.Add(kvp.Key);
            }
        }
        foreach (Renderer r in toRestore)
        {
            RestoreRenderer(r);
        }

        foreach (Renderer r in stillBlocking)
        {
            if (!s_tintedRenderers.ContainsKey(r))
            {
                TintRenderer(r);
            }
        }
    }

    /// <summary>
    /// Restores every currently-tinted renderer. Call when the build ghost
    /// is destroyed or build mode ends, so nothing is left stuck red.
    /// </summary>
    public static void ClearBlockerTints()
    {
        List<Renderer> renderers = new List<Renderer>(s_tintedRenderers.Keys);
        foreach (Renderer r in renderers)
        {
            if (r != null) RestoreRenderer(r);
        }
        s_tintedRenderers.Clear();
    }

    private static void TintRenderer(Renderer r)
    {
        MaterialPropertyBlock original = new MaterialPropertyBlock();
        r.GetPropertyBlock(original);
        s_tintedRenderers[r] = original;

        MaterialPropertyBlock modified = new MaterialPropertyBlock();
        r.GetPropertyBlock(modified);
        modified.SetColor(EmissionColor, Color.red);
        r.SetPropertyBlock(modified);
    }

    private static void RestoreRenderer(Renderer r)
    {
        if (s_tintedRenderers.TryGetValue(r, out MaterialPropertyBlock original))
        {
            r.SetPropertyBlock(original);
        }
        s_tintedRenderers.Remove(r);
    }
}