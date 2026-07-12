using UnityEngine;

public static class GhostPreviewFactory
{
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

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
}