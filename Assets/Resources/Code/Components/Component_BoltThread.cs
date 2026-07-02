using UnityEngine;

//Attach this to the cylinder/threads of the bolt so the game knows how long the cylinder is and can move the bolt in/out of a slot
//
public class Component_BoltThread : MonoBehaviour
{
    public float GetBoltLength()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"No MeshFilter found on {gameObject.name}. Cannot calculate true size.");
            return 0f;
        }
        Vector3 rawSize = meshFilter.sharedMesh.bounds.size;
        Vector3 worldScale = transform.lossyScale;
        // Take the largest scaled axis rather than assuming Y — bolts modeled along X or Z won't silently break.
        Vector3 scaledSize = Vector3.Scale(rawSize, worldScale);
        return Mathf.Max(scaledSize.x, scaledSize.y, scaledSize.z);
    }
}