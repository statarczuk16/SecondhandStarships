using UnityEngine;

public class Component_PrimitiveMountPoint : MonoBehaviour
{
    public Transform GetMountPoint()
    {
        return this.gameObject.transform;
    }
}
