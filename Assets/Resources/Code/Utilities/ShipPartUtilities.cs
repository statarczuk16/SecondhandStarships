using UnityEngine;

public static class ShipPartUtilities
{
    /// <summary>
    /// Calculates the correct world position and rotation needed to spawn a child prefab
    /// so that its child MountPoint matches the parent MountPoint transform precisely.
    /// </summary>
    /// <param name="parentMount">The MountPoint component child inside the slot.</param>
    /// <param name="childPrefabMount">The MountPoint component child inside the uninstantiated bolt prefab.</param>
    /// <param name="outPosition">The final world position to pass into Instantiate.</param>
    /// <param name="outRotation">The final world rotation to pass into Instantiate.</param>
    public static void CalculateAlignmentTransform(
        Component_MountPoint parentMount,
        Component_MountPoint childPrefabMount,
        out Vector3 outPosition,
        out Quaternion outRotation)
    {
        if (parentMount == null || childPrefabMount == null)
        {
            outPosition = Vector3.zero;
            outRotation = Quaternion.identity;
            return;
        }

        Transform parentMountPoint = parentMount.GetMountPoint();
        Transform childMountPoint = childPrefabMount.GetMountPoint();

        // The root transform of the prefab asset
        Transform childRoot = childPrefabMount.transform.root;

        // 1. Calculate final world rotation
        // Find the rotation of the root relative to its child anchor, then map it onto the target anchor
        Quaternion rootRelativeRotation = Quaternion.Inverse(childMountPoint.rotation) * childRoot.rotation;
        outRotation = parentMountPoint.rotation * rootRelativeRotation;

        // 2. Calculate final world position
        // Find the directional vector from the child anchor to its root pivot in the prefab layout
        Vector3 anchorToRootOffset = childRoot.position - childMountPoint.position;

        // Rotate that directional vector to match our newly calculated target world orientation
        Vector3 rotatedOffset = outRotation * (Quaternion.Inverse(childRoot.rotation) * anchorToRootOffset);

        // Final world placement is the target anchor shifted by that rotated directional layout offset
        outPosition = parentMountPoint.position + rotatedOffset;
    }

    public static Component_ShipPart Get(GameObject prefab)
    {
        if (prefab == null) return null;
        return prefab.GetComponent<Component_ShipPart>();
    }

    // Walk up from the hit transform (inclusive) to find the nearest ancestor carrying
    // a Component_PrefabBoundary. This identifies which prefab instance was actually hit,
    // even when that prefab is nested inside other prefabs.
    public static Transform FindOwningPrefabBoundary(Transform start)
    {
        Transform t = start;
        while (t != null)
        {
            if (t.GetComponent<Component_PrefabBoundary>() != null)
            {
                return t;
            }
            t = t.parent;
        }
        return null;
    }

    // Search this prefab's own hierarchy for an IInteractable, refusing to descend into
    // any nested child prefab (its own Component_PrefabBoundary marks a different instance).
    public static IInteractable FindInteractableWithinBoundary(Transform boundaryRoot)
    {
        IInteractable direct = boundaryRoot.GetComponent<IInteractable>();
        if (direct != null) return direct;

        for (int i = 0; i < boundaryRoot.childCount; i++)
        {
            Transform child = boundaryRoot.GetChild(i);

            // Don't cross into a nested prefab's own boundary — that's a distant
            // child belonging to a different prefab instance.
            if (child.GetComponent<Component_PrefabBoundary>() != null)
            {
                continue;
            }

            IInteractable found = FindInteractableWithinBoundary(child);
            if (found != null) return found;
        }

        return null;
    }

    public static T FindComponentWithinPrefab<T>(Transform boundaryRoot) where T : class
    {
        // 1. Check if the component exists right on the current object
        T direct = boundaryRoot.GetComponent<T>();
        if (direct != null) return direct;

        // 2. Otherwise, look through the children
        for (int i = 0; i < boundaryRoot.childCount; i++)
        {
            Transform child = boundaryRoot.GetChild(i);

            // Don't cross into a nested prefab's own boundary
            if (child.GetComponent<Component_PrefabBoundary>() != null)
            {
                continue;
            }

            // Recursively search this child branch
            T found = FindComponentWithinPrefab<T>(child);
            if (found != null) return found;
        }

        return null;
    }
}