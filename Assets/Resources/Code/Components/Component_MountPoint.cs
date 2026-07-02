using UnityEngine;

public class Component_MountPoint : MonoBehaviour
{
    public Transform GetMountPoint()
    {
        return this.gameObject.transform;
    }
}
