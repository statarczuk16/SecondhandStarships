using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Self-contained bolt connector. Lives on the PART prefab (part owns attachment type).
/// Owns both "slot" placement (this transform is the mount point) and the bolt's own
/// installation state/visuals. Spawns its bolt visual at runtime, destroys it when
/// fully backed out, and notifies its owning part on state changes.
/// </summary>
public class Component_BoltSlot : MonoBehaviour, IPartConnector, IInteractable, IHighlightable
{
    [Header("Setup")]
    [SerializeField] private HighlightableRenderer highlightRenderer;
    [SerializeField] private Component_ShipPart partOwner;
    [SerializeField] private GameObject boltVisualPrefab; // visual-only, must contain Component_BoltThread
    [SerializeField] private EquipmentType requiredTool = EquipmentType.SOCKET_WRENCH;
    [SerializeField] private float boltFullInstallRotations = 5f; // full turns from 0% to 100%
    [Header("Runtime State")]
    [SerializeField] private int m_installation_progress = 0; // 0 = loose, 100 = fully tight
    [SerializeField] private InstallationState m_installation_state = InstallationState.UNINSTALLED;

    private GameObject m_spawnedBoltVisual;
    private float m_screwLength = 0f;



    public string GetInteractionLabel(Controller_Equipment controller)
    {
        if (controller.GetEquippedTool() != requiredTool)
        {
            return $"//BOLT -> requires {requiredTool}";
        }
        return $"//BOLT install_progress = {m_installation_progress}";
    }

    public Transform InteractionPoint => transform;
    public InstallationState GetInstallState() => m_installation_state;
    public int GetInstallationProgress() => m_installation_progress;

    private void OnValidate()
    {
        if (boltVisualPrefab != null)
        {
            Component_BoltThread threadCheck = boltVisualPrefab.GetComponentInChildren<Component_BoltThread>();
            if (threadCheck == null)
            {
                Debug.LogError($"{gameObject.name} needs a boltVisualPrefab with a Component_BoltThread");
                boltVisualPrefab = null;
            }
        }
    }

    public bool SetOwner(Component_ShipPart owner)
    {
        partOwner = owner;
        return true;
    }

    /// <summary>
    /// Called by the owning part when it's placed into its hull slot. Resets this
    /// connector to loose and spawns its bolt visual at installation_progress = 0.
    /// </summary>
    public void InitializeConnector()
    {
        m_installation_progress = 0;
        m_installation_state = InstallationState.UNINSTALLED;
        DespawnBoltVisual();
    }

    private void SpawnBoltVisual()
    {
        if (boltVisualPrefab == null || m_spawnedBoltVisual != null)
        {
            return;
        }

        m_spawnedBoltVisual = GameObject.Instantiate(boltVisualPrefab, transform);
        m_spawnedBoltVisual.transform.localPosition = Vector3.zero;
        m_spawnedBoltVisual.transform.localRotation = Quaternion.identity;

        Component_BoltThread thread = m_spawnedBoltVisual.GetComponentInChildren<Component_BoltThread>();
        m_screwLength = thread != null ? thread.GetBoltLength() : 0f;

        PositionBoltVisual();
    }

    private void PositionBoltVisual()
    {
        if (m_spawnedBoltVisual == null) return;

        float current_percent = m_installation_progress / 100f;
        float absolute_depth = current_percent * m_screwLength;

        m_spawnedBoltVisual.transform.localPosition = Vector3.forward * absolute_depth;
        m_spawnedBoltVisual.transform.localRotation =
            Quaternion.Euler(0f, 0f, current_percent * boltFullInstallRotations * 360f);
    }

    /// <summary>
    /// Advances or reverses installation progress. Called by the wrench minigame.
    /// </summary>
    public void InstallationUpdate(int amount)
    {
        m_installation_progress = Mathf.Clamp(m_installation_progress + amount, 0, 100);
        InstallationState prev_state = m_installation_state;
        if (m_installation_progress <= 0)
        {
            m_installation_state = InstallationState.UNINSTALLED;
            DespawnBoltVisual();
        }
        else if (m_installation_progress >= 100)
        {
            m_installation_state = InstallationState.INSTALLED;
            PositionBoltVisual();
        }
        else
        {
            m_installation_state = InstallationState.INSTALLING;
            PositionBoltVisual();
        }

        if(prev_state != m_installation_state)
        {
            partOwner?.OnConnectorStatusChanged();
        }
    }

    private void DespawnBoltVisual()
    {
        if (m_spawnedBoltVisual != null)
        {
            GameObject.Destroy(m_spawnedBoltVisual);
            m_spawnedBoltVisual = null;
        }
    }

    // --- IInteractable ---

    public bool CanInteract(Controller_Equipment controller)
    {
        return requiredTool == controller.GetEquippedTool();
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        SetHighlight(CanInteract(controller) ? InteractionHighlightState.VALID : InteractionHighlightState.INVALID);
    }

    public void OnHoverExit(Controller_Equipment controller)
    {
        SetHighlight(InteractionHighlightState.NONE);
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController, RaycastHit hitInfo) 
    { }

    public void OnInteract(Controller_Equipment equipmentController)
    {
        if (!CanInteract(equipmentController))
        {
            return;
        }

        if (m_installation_state == InstallationState.UNINSTALLED)
        {
            SpawnBoltVisual();
        }

        var mini_game_goal = new HashSet<InstallationState>
        {
            InstallationState.INSTALLED,
            InstallationState.UNINSTALLED
        };
        MiniGame_Wrench minigame = new MiniGame_Wrench(this, EquipmentType.SOCKET_WRENCH, mini_game_goal);
        minigame.SetOutcomes(InputMode.MovingMode, null, null, null);
        equipmentController.startMiniGame(minigame);
    }

    // --- IHighlightable ---

    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        highlightRenderer?.SetHighlight(state);
    }

    public EquipmentType RequiredTool()
    {
        return requiredTool;
    }
}