using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;


public enum FastenerState
{
    NOT_INSTALLED,
    INSTALLING,
    SECURE
}
//screw hole, nail hole, cuttable surface, etc
public interface IAttachmentSlot : IInteractable, IHighlightable
{
    bool FastenerInstalled { get; }          // true = still holding the part on
    IAttachmentFastener RequiredFastener { get; }
    bool SetOwner(Component_ShipPart owner);
    //call these so I know if my slot has been filled or unfilled (bolt is in the bolt hole, or not)
    void NotifyFastenerUninstalled(IAttachmentFastener fastener);
    void NotifyFastenerInstalled(IAttachmentFastener fastener);
    //I call these to tell my parent that I am filled or unfilled (so it knows if all the bolts holding it in place are gone or not)
    void NotifyParentThatIAmFastened(IAttachmentSlot slot);
    void NotifyParentThatIAmUnFastened(IAttachmentSlot slot);
}

//thing that goes in the slot. screw, nail, metal patch, etc
public interface IAttachmentFastener : IInteractable, IHighlightable
{
    FastenerState GetInstallState();
    void InstallationUpdate(IAttachmentSlot slot, int installation_progress);
    EquipmentType RequiredTool();
}

