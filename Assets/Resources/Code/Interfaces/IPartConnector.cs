using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;


public enum InstallationState
{
    UNINSTALLED,
    INSTALLING,
    INSTALLED
}

public interface IPartConnector : IInteractable, IHighlightable
{
    InstallationState GetInstallState();
    EquipmentType RequiredTool();
    bool SetOwner(Component_ShipPart owner);
    void InitializeConnector();
}
