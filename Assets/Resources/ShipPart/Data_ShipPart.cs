using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Data_ShipPart
{
    public string part_name = "part_name_uninit";
    public GameObject prefab;
    public ShipSlotSize slot_size;
    public bool is_installed;

    // Deep-copies the list so multiple inventory entries built from the
    // same prefab asset don't end up sharing one fastenerInstalledStates list.
    public Data_ShipPart Clone()
    {
        return new Data_ShipPart
        {
            part_name = part_name,
            prefab = prefab,
            slot_size = slot_size,
            is_installed = is_installed,
        };
    }
}