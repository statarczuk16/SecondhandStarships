using System.Collections.Generic;
using UnityEngine;

[System.Serializable]



public class Data_ShipModule
{
    public string part_name = "part_name_uninit";
    public GameObject prefab;
    public InstallationState install_state;

   
    public Data_ShipModule Clone()
    {
        return new Data_ShipModule
        {
            part_name = part_name,
            prefab = prefab,
            install_state = install_state,
        };
    }
}