using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class BoltComponent : MonoBehaviour, IAttachmentFastener, IInteractable, IHighlightable
{
    [SerializeField] private HighlightableRenderer highlightRenderer;
    [SerializeField] private int m_installation_progress = 0; // 0 = loose, 100 = fully tight
    [SerializeField] private FastenerState m_installation_state = FastenerState.NOT_INSTALLED;
    [SerializeField] public float screw_length = 0f;
    public EquipmentType m_required_tool = EquipmentType.Wrench;
    public IAttachmentSlot m_fastener_slot;
    public float depth_into_slot = 0f;


    public Transform InteractionPoint => transform;

    // --- Core Logic ---

    public void Awake()
    {
        Component_BoltThread temp = this.gameObject.GetComponentInChildren<Component_BoltThread>();
        this.screw_length = temp.GetBoltLength();
    }

    public void InstallationUpdate(IAttachmentSlot slot, int amount)
    {
        int previous = m_installation_progress;
        if (slot != m_fastener_slot && m_installation_state != FastenerState.NOT_INSTALLED)
        {
            Debug.LogError($"Trying to install bolt {this.gameObject.name} into a slot when it's already installed in another slot");
            return;
        }
        m_fastener_slot = slot;
        m_installation_progress = Mathf.Clamp(m_installation_progress + amount, 0, 100);
        if (m_installation_progress <= 0f)
        {
            m_installation_state = FastenerState.NOT_INSTALLED;
            slot.NotifyFastenerUninstalled(this);
            

            //TODO end mini game
        }
        else if (m_installation_progress >= 100f)
        {
            m_installation_state = FastenerState.SECURE;
            slot.NotifyFastenerInstalled(this);

        }
        else
        {
            m_installation_state = FastenerState.INSTALLING;
        }

        float current_percent = m_installation_progress / 100f;

        // 2. Calculate the exact physical depth it should be at right now
        float absolute_depth = current_percent * screw_length;

        // 3. Snap the local Z-axis to that exact depth. 
        // (Assuming Z=0 is fully uninstalled, and Z=screw_length is fully installed)
        transform.localPosition = new Vector3(
            transform.localPosition.x,
            transform.localPosition.y,
            absolute_depth
        );


    }

    // --- IAttachmentFastener Implementation ---


    public EquipmentType RequiredTool()
    {
        return m_required_tool;
    }


    // --- IInteractable Implementation ---

    public bool CanInteract(Controller_Equipment controller)
    {
        if (m_installation_state == FastenerState.NOT_INSTALLED)
        {
            // If I'm not installed in a slot, I can be picked up
            return true;
        }
        else
        {
            // If I am installed in a slot or being installed, you need the right tool to uninstall me
            return m_required_tool == controller.GetEquippedTool();
        }
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

    public void OnHoverExit()
    {
        SetHighlight(InteractionHighlightState.NONE);
    }

    public void OnInteract(Controller_Equipment equipment_controller)
    {
        if(GetInstallState() == FastenerState.NOT_INSTALLED)
        {
            GameObject.Destroy(this.gameObject); // Destroys the GameObject
            //TODO inventory system.. we are picking up an unattached bolt
        }
        else
        {
            //Start the mini game for bolts using the slot this bolt is in
            equipment_controller.startMiniGame(new MiniGame_Wrench(this, EquipmentType.Wrench));
        }
    }

    public float GetInstallationProgress()
    {
        return m_installation_progress;
    }


    public void SetHighlight(InteractionHighlightState state)
    {
        if (highlightRenderer != null)
        {
            highlightRenderer.SetHighlight(state);
        }
    }

    public FastenerState GetInstallState()
    {
        return m_installation_state;
    }
}