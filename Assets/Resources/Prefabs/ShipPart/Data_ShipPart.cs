using System.Collections.Generic;
using UnityEngine;

[System.Serializable]



public class Data_ShipPart
{
    public string part_name = "part_name_uninit";
    public GameObject prefab;
    public InstallationState install_state;

   
    public Data_ShipPart Clone()
    {
        return new Data_ShipPart
        {
            part_name = part_name,
            prefab = prefab,
            install_state = install_state,
        };
    }
}