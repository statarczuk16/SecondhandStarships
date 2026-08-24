using System;
using System.Collections.Generic;
using UnityEngine;

public class Component_BuildableSurface : MonoBehaviour, IInteractable, IHighlightable
{
    private GameObject currentGhost;
    private Data_ShipModule _currentGhostModuleData;
    [SerializeField] private Component_ShipChunk m_parent_chunk;
    private bool m_placement_blocked;
    public string GetInteractionLabel(Controller_Equipment controller)
    {
        if (controller.GetEquippedTool() != EquipmentType.SHIP_BUILDER)
        {
            return $"//SHIP_SURFACE {this.m_parent_chunk.name} -> EQUIP {EquipmentType.SHIP_BUILDER} TO INSTALL MODULES";
        }
        else
        {
            return $"//SHIP_SURFACE {this.m_parent_chunk.name}";
        }
    }

    public Transform InteractionPoint => transform;
    [Header("Debug")]
    [SerializeField] private List<GameObject> m_current_blockers = new List<GameObject>();

    public void Awake()
    {
        if(!m_parent_chunk)
        {
            m_parent_chunk = this.transform.GetComponentInParent<Component_ShipChunk>();
            if (m_parent_chunk == null)
            {
                throw new Exception("Needs to have parent" + this.name);
            }
        }
        
    }

    public bool CanInteract(Controller_Equipment controller)
    {
        if (!controller.BuildMode()) return false;
        return controller.GetEquippedPart() != null;
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        if (CanInteract(controller))
        {
            SetHighlight(InteractionHighlightState.VALID, controller);
        }
    }

    public void OnHoverUpdate(Controller_Equipment controller, RaycastHit hitInfo)
    {
        if (!CanInteract(controller))
        {
            ClearGhost();
            return;
        }
        PlaceGhost(controller.GetEquippedPart(), hitInfo, controller);
    }

    public void OnHoverExit(Controller_Equipment controller)
    {
        SetHighlight(InteractionHighlightState.NONE, controller);
    }

    public void StartPartInstallation(Component_ShipPart part)
    {
        part.StartInstall(this);
    }

    public void OnPartUninstalled(Component_ShipPart part)
    {
        this.m_parent_chunk.OnPartUninstalled(part);
    }

    public void OnPartInstalled(Component_ShipPart part)
    {
        this.m_parent_chunk.InstallPart(part);
    }

    public void OnInteract(Controller_Equipment controller)
    {
        if (!CanInteract(controller)) return;
        if (m_placement_blocked) return;
        Data_ShipModule moduleData = controller.GetEquippedPart();

        GameObject spawnedPart = Instantiate(moduleData.prefab, currentGhost.transform.position, currentGhost.transform.rotation);
        Component_ShipPart part_component = spawnedPart.GetComponent<Component_ShipPart>();
        part_component.SetData(moduleData);
        StartPartInstallation(part_component);
        controller.RemovePartFromInventory(moduleData);
        ClearGhost();
    }

    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        if (state != InteractionHighlightState.VALID)
        {
            ClearGhost();
        }
        // Ghost itself is created lazily in PlaceGhost/OnHoverUpdate, since we need
        // hit position before we can spawn it usefully.
    }

    private void PlaceGhost(Data_ShipModule dataShipModule, RaycastHit hitInfo, Controller_Equipment controller)
    {
        if (dataShipModule == null)
        {
            ClearGhost();
            return;
        }

        if (currentGhost == null || _currentGhostModuleData != dataShipModule)
        {
            ClearGhost();
            currentGhost = GhostPreviewFactory.CreateGhost(dataShipModule.prefab, null, Color.green);
            _currentGhostModuleData = dataShipModule;
        }

        currentGhost.transform.position = hitInfo.point;
        currentGhost.transform.rotation = GetGhostRotation(hitInfo, controller);

        UpdatePlacementValidity();
    }
    
    /// <summary>
    /// Orients the ghost against the surface's up/forward axes, but flips each
    /// axis toward the player if they're viewing the surface from the "far"
    /// side (e.g. standing above a ceiling panel, or behind a wall surface).
    /// </summary>
    private Quaternion GetGhostRotation(RaycastHit hitInfo, Controller_Equipment controller)
    {
        Vector3 surfaceUp = transform.up;
        Vector3 surfaceForward = transform.forward;

        Camera playerCamera = controller.transform.GetComponentInChildren<Camera>();
        
    
        Vector3 toCamera = (playerCamera.transform.position - hitInfo.point).normalized;
    
        Vector3 resolvedUp = Vector3.Dot(toCamera, surfaceUp) < 0f ? -surfaceUp : surfaceUp;
        Vector3 resolvedForward = Vector3.Dot(toCamera, surfaceForward) < 0f ? -surfaceForward : surfaceForward;
    
        // LookRotation only needs an approximate up axis - it will project it
        // onto the plane perpendicular to forward automatically, so resolvedUp
        // and resolvedForward don't need to be perfectly orthogonal here.
        return Quaternion.LookRotation(resolvedForward, resolvedUp);
    }

    private void UpdatePlacementValidity()
    {
        Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            Collider collider = renderers[i].gameObject.transform.GetComponent<Collider>();
            if (collider && collider.enabled && !collider.isTrigger)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        Collider[] overlaps = Physics.OverlapBox(
            bounds.center, bounds.extents, currentGhost.transform.rotation,
            ~0, QueryTriggerInteraction.Ignore);

        Transform myBoundary = ShipPartUtilities.FindOwningPrefabBoundary(this.transform);

        HashSet<GameObject> blockers = new HashSet<GameObject>();
        m_current_blockers.Clear();
        foreach (Collider col in overlaps)
        {
            Transform candidateBoundary = ShipPartUtilities.FindOwningPrefabBoundary(col.transform);

            // Skip anything belonging to the surface's own prefab (the floor/hull
            // the surface sits on shouldn't block itself).
            if (candidateBoundary != null && myBoundary != null && candidateBoundary == myBoundary) continue;
            if (candidateBoundary == null && col.gameObject == this.gameObject) continue;

            if (col.GetComponentInChildren<Renderer>() == null || col.GetComponentInChildren<Renderer>().enabled == false)
            {
                TopicLogger.Log(LogTopic.Interaction, LogLevel.WARN,
                    $"Blocker {col.gameObject.name} has a collider but no visible renderer in children.");
            }
            GameObject blockerRoot = candidateBoundary != null ? candidateBoundary.gameObject : col.gameObject;
            blockers.Add(blockerRoot);
            m_current_blockers.Add(col.gameObject);
        }

        m_placement_blocked = blockers.Count > 0;
        GhostPreviewFactory.UpdateBlockerTints(blockers);
        GhostPreviewFactory.ApplyTint(currentGhost, m_placement_blocked ? Color.red : Color.green);
        
        
        
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (m_parent_chunk != null) return;
        Component_ShipChunk found = GetComponentInParent<Component_ShipChunk>();
        if (found != null)
        {
            m_parent_chunk = found;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    private Bounds GetGhostBounds()
    {
        Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(currentGhost.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    private void ClearGhost()
    {
        if (currentGhost != null)
        {
            GhostPreviewFactory.Destroy(currentGhost);
            currentGhost = null;
        }
        _currentGhostModuleData = null;
        GhostPreviewFactory.ClearBlockerTints();
        m_current_blockers.Clear();
    }

    public void InstallToChunk(Component_ShipChunk componentShipChunk)
    {
        
    }
}