using System.Runtime.CompilerServices;
using UnityEngine;

public class BoltSlotComponent : MonoBehaviour, IAttachmentSlot
{
    [SerializeField] private HighlightableRenderer highlightRenderer;
    [SerializeField] private Component_ShipPart partOwner;
    [SerializeField] private GameObject defaultFastenerPrefab; //make this spawn on the slot when fastening

    private IAttachmentFastener currentFastener = null;

    // --- IInteractable / IHighlightable Properties ---

    private void OnValidate()
    {
        if (defaultFastenerPrefab != null)
        {
            // Verify the prefab contains the required interface
            BoltComponent interfaceCheck = defaultFastenerPrefab.GetComponent<BoltComponent>();

            if (interfaceCheck == null)
            {
                Debug.LogError($"{this.gameObject.name} needs DefaultFastener in its slot with a BoltComponent");
                defaultFastenerPrefab = null;
            }
        }
    }

    public Transform InteractionPoint
    {
        get { return transform; }
    }

    // --- IAttachmentSlot Properties ---

    public bool FastenerInstalled
    {
        get
        {
            return currentFastener != null && currentFastener.GetInstallState() == FastenerState.SECURE;
        }
    }

    public IAttachmentFastener RequiredFastener
    {
        get { return null; } // Customize this if you want to enforce specific bolt types
    }

    // --- Core Logic & Initialization ---

    public bool SetOwner(Component_ShipPart owner)
    {
        partOwner = owner;
        return true;
    }

    // --- Fastener Notification Callbacks (Called by BoltComponent) ---

    public void NotifyFastenerInstalled(IAttachmentFastener fastener)
    {
        currentFastener = fastener;
        NotifyParentThatIAmFastened(this);
    }

    public void NotifyFastenerUninstalled(IAttachmentFastener fastener)
    {
        if (currentFastener == fastener)
        {
            currentFastener = null;
            NotifyParentThatIAmUnFastened(this);
        }
    }

    // --- Parent Notification Logic ---

    public void NotifyParentThatIAmFastened(IAttachmentSlot slot)
    {
        if (partOwner != null)
        {
            //partOwner.OnSlotFastened(slot);
        }
    }

    public void NotifyParentThatIAmUnFastened(IAttachmentSlot slot)
    {
        if (partOwner != null)
        {
            partOwner.NotifyAttachmentCleared(slot);
        }
    }

    // --- IInteractable Implementation ---

    public bool CanInteract(Controller_Equipment controller)
    {
        // If a bolt is already here, you interact with the bolt instead of the slot
        if (currentFastener != null)
        {
            return false;
        }

        // Example condition: check if player is holding a bolt to insert
        return controller.GetEquippedTool() == EquipmentType.SOCKET_WRENCH;
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        if (CanInteract(controller))
        {
            SetHighlight(InteractionHighlightState.VALID);
        }
        else
        {
            SetHighlight(InteractionHighlightState.INVALID);
        }
    }

    public void OnHoverExit(Controller_Equipment controller)
    {
        SetHighlight(InteractionHighlightState.NONE);
    }

    public void OnInteract(Controller_Equipment activeTool)
    {
        if (!CanInteract(activeTool))
        {
            return;
        }

        Component_MountPoint parentMount = this.gameObject.GetComponentInChildren<Component_MountPoint>();


        if (parentMount == null || defaultFastenerPrefab == null)
        {
            return;
        }

        // Grab the MountPoint script sitting inside the Prefab hierarchy
        Component_MountPoint childMount = defaultFastenerPrefab.GetComponentInChildren<Component_MountPoint>();

        if (childMount != null)
        {
            Vector3 spawnPosition;
            Quaternion spawnRotation;

            // 1. Query our vector math blueprint
            ShipPartUtilities.CalculateAlignmentTransform(
                parentMount,
                childMount,
                out spawnPosition,
                out spawnRotation
            );

            // 2. Spawn directly into place (no visual snapping/flicker)
            GameObject spawnedBoltItem = GameObject.Instantiate(defaultFastenerPrefab, spawnPosition, spawnRotation);

            // 3. Parent it to the slot's mount point transform so it moves smoothly with the machinery
            spawnedBoltItem.transform.SetParent(parentMount.GetMountPoint());

            // 4. Update core interaction logic tracking
            BoltComponent boltComp = spawnedBoltItem.GetComponent<BoltComponent>();
            currentFastener = boltComp;
            currentFastener.InstallationUpdate(this, 1);//start the bolt 25% of the way tight so the minigame can start without ending immediately


            
        }
    }

    // --- IHighlightable Implementation ---

    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        if (highlightRenderer != null)
        {
            highlightRenderer.SetHighlight(state);
        }
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController)
    {
        throw new System.NotImplementedException();
    }
}