using UnityEngine;

public static class ShipPartUtilities
{
    
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