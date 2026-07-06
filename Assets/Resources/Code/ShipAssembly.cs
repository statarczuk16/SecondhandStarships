using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class ShipDefinition : ScriptableObject
{
    public string shipName;
    public GameObject shipPrefab; // prefab with PartComponents + fasteners pre-wired
    public int basePurchasePrice;
}

public class ShipInstance : MonoBehaviour
{
    List<Component_ShipPart> parts;
    void Awake() => parts = GetComponentsInChildren<Component_ShipPart>().ToList();
}