using System;
using System.Collections.Generic;
using UnityEngine;

public class Component_BuildableSurface : MonoBehaviour, IInteractable, IHighlightable
{
    private GameObject currentGhost;
    private Data_ShipModule _currentGhostModuleData;
    private Component_Ship m_parent_ship;
    private bool m_placement_blocked;
    public Transform InteractionPoint => transform;
    [Header("Debug")]
    [SerializeField] private List<GameObject> m_current_blockers = new List<GameObject>();

    public void Awake()
    {
        m_parent_ship = this.transform.GetComponentInParent<Component_Ship>();
        if(m_parent_ship == null)
        {
            throw new Exception("Needs to have parent" + this.name);
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
        PlaceGhost(controller.GetEquippedPart(), hitInfo);
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
        this.m_parent_ship.OnPartUninstalled(part);
    }

    public void OnPartInstalled(Component_ShipPart part)
    {
        this.m_parent_ship.InstallPart(part);
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

    private void PlaceGhost(Data_ShipModule dataShipModule, RaycastHit hitInfo)
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
        currentGhost.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);

        UpdatePlacementValidity();
    }

    private void UpdatePlacementValidity()
    {
        Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

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

            if (col.GetComponentInChildren<Renderer>() == null)
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
}