using System;

[System.Serializable]
public class Data_ShipPartSlot
{
    public Guid guid; // stable identifier — assign in Inspector, don't rely on index/name
    public ShipSlotSize max_allowed_size;
    public bool filled;
}