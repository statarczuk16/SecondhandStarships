using UnityEngine;

public static class MountPointUtility
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
}