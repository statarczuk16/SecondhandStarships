using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool with two independent placement modes, shown side by side:
///
/// LEFT  - places arbitrary prefabs by hand while snapping them to
///         Component_MountPoint markers in the scene (unchanged from before).
///
/// RIGHT - places Component_ShipPart prefabs onto whatever Component_BuildableSurface
///         is under the cursor, oriented the same way Component_BuildableSurface's own
///         runtime ghost orients itself (surface up/forward, flipped toward the
///         viewer). On click the part is instantiated and immediately marked fully
///         installed via Component_ShipPart.PlaceAsInstalled(), so you don't have to
///         walk through each connector's minigame by hand while iterating in editor.
///
/// Only one placement mode can be active at a time; entering one exits the other.
/// </summary>
public class Editor_PrefabMountPlacer : EditorWindow
{
    private const float MaxRaycastDistance = 1000f;

    private enum PlacementMode { None, MountPrefab, ShipPart }
    private PlacementMode m_placementMode = PlacementMode.None;

    private static readonly Color FreeTint = new Color(0.4f, 0.8f, 1f, 1f);
    private static readonly Color SnappedTint = new Color(0.3f, 1f, 0.4f, 1f);
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // ---- Mount-point prefab placement (left panel) ----

    private List<GameObject> m_availablePrefabs = new List<GameObject>();
    private GameObject m_selectedPrefab;
    private Vector2 m_scrollPos;

    private float m_snapRadius = 1.5f;
    private bool m_parentToMountTarget = true;

    private GameObject m_ghost;
    private GameObject m_ghostSourcePrefab;
    private Quaternion m_ghostBaseRotation;

    private bool m_currentlySnapped;
    private Component_MountPoint m_snappedSceneMount;
    private string m_bestGhostMountLocalId;
    private bool m_hasValidHit;

    // ---- Ship part placement (right panel) ----

    private List<GameObject> m_availableShipParts = new List<GameObject>();
    private GameObject m_selectedShipPart;
    private Vector2 m_shipPartScrollPos;

    private GameObject m_shipPartGhost;
    private GameObject m_shipPartGhostSourcePrefab;
    private Component_BuildableSurface m_shipPartHoveredSurface;
    private bool m_shipPartHasValidHit;

    [MenuItem("Tools/Ship Builder/Prefab Mount Placer")]
    private static void Open()
    {
        GetWindow<Editor_PrefabMountPlacer>("Mount Placer");
    }

    private void OnEnable()
    {
        RefreshPrefabList();
        RefreshShipPartList();
    }

    private void OnDisable()
    {
        SetPlacementMode(PlacementMode.None);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(Mathf.Max(220f, position.width * 0.5f - 6f)));
        DrawMountPrefabPanel();
        EditorGUILayout.EndVertical();

        GUILayout.Box(GUIContent.none, GUILayout.Width(1f), GUILayout.ExpandHeight(true));

        EditorGUILayout.BeginVertical();
        DrawShipPartPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void SetPlacementMode(PlacementMode mode)
    {
        if (mode == m_placementMode) return;

        if (m_placementMode != PlacementMode.None)
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            DestroyGhost();
            DestroyShipPartGhost();
            m_hasValidHit = false;
            m_shipPartHasValidHit = false;
        }

        m_placementMode = mode;

        if (mode != PlacementMode.None)
        {
            // Avoid the built-in Move/Rotate gizmo intercepting our placement clicks.
            Selection.activeGameObject = null;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        SceneView.RepaintAll();
        Repaint();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        switch (m_placementMode)
        {
            case PlacementMode.MountPrefab:
                OnSceneGUI_MountPrefab(sceneView);
                break;
            case PlacementMode.ShipPart:
                OnSceneGUI_ShipPart(sceneView);
                break;
        }
    }

    // ===================================================================
    // Mount-point prefab placement (left panel)
    // ===================================================================

    private void DrawMountPrefabPanel()
    {
        EditorGUILayout.LabelField("Mountable Prefabs", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh List"))
        {
            RefreshPrefabList();
        }

        m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, "box", GUILayout.Height(180));
        if (m_availablePrefabs.Count == 0)
        {
            EditorGUILayout.HelpBox("No prefabs found containing a Component_MountPoint.", MessageType.Info);
        }
        foreach (GameObject prefab in m_availablePrefabs)
        {
            bool isSelected = prefab == m_selectedPrefab;
            GUI.backgroundColor = isSelected ? new Color(0.5f, 0.8f, 1f) : Color.white;
            if (GUILayout.Button(prefab.name))
            {
                m_selectedPrefab = prefab;
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        m_snapRadius = Mathf.Max(0.01f, EditorGUILayout.FloatField("Snap Radius", m_snapRadius));
        m_parentToMountTarget = EditorGUILayout.Toggle(
            new GUIContent("Parent To Mount Target", "When a mount snap occurs, parent the placed object under the scene mount point's GameObject."),
            m_parentToMountTarget);

        EditorGUILayout.Space();

        bool active = m_placementMode == PlacementMode.MountPrefab;
        using (new EditorGUI.DisabledScope(m_selectedPrefab == null))
        {
            GUI.backgroundColor = active ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f);
            string label = active ? "Exit Placement Mode (Esc)" : "Enter Placement Mode";
            if (GUILayout.Button(label, GUILayout.Height(30)))
            {
                SetPlacementMode(active ? PlacementMode.None : PlacementMode.MountPrefab);
            }
            GUI.backgroundColor = Color.white;
        }

        if (active)
        {
            EditorGUILayout.HelpBox(
                "Left-click in the Scene view to place " + (m_selectedPrefab != null ? m_selectedPrefab.name : "") +
                ". Placement mode stays active for repeated placement.",
                MessageType.Info);
        }
    }

    private void RefreshPrefabList()
    {
        m_availablePrefabs.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;

            if (asset.GetComponentInChildren<Component_MountPoint>(true) != null)
            {
                EnsureMountPointIdsAssigned(asset);
                m_availablePrefabs.Add(asset);
            }
        }
    }

    /// <summary>
    /// Opens the prefab asset's contents and assigns a LocalId to any
    /// Component_MountPoint missing one, saving back to the asset if anything
    /// changed. Run once per prefab (on list refresh) so every future instance
    /// made from this asset - ghost or real - inherits identical, matching IDs.
    /// </summary>
    private static void EnsureMountPointIdsAssigned(GameObject prefabAsset)
    {
        string path = AssetDatabase.GetAssetPath(prefabAsset);
        if (string.IsNullOrEmpty(path)) return;

        GameObject contentsRoot = PrefabUtility.LoadPrefabContents(path);
        try
        {
            bool changed = false;
            foreach (Component_MountPoint mount in contentsRoot.GetComponentsInChildren<Component_MountPoint>(true))
            {
                if (!mount.HasLocalId)
                {
                    mount.AssignIdIfMissing();
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(contentsRoot, path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contentsRoot);
        }
    }

    private void OnSceneGUI_MountPrefab(SceneView sceneView)
    {
        if (m_selectedPrefab == null)
        {
            return;
        }

        sceneView.wantsMouseMove = true;

        // Claim the scene view control so a left click here places instead of
        // (de)selecting whatever object happens to be under the cursor.
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);

        Event e = Event.current;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        m_hasValidHit = Physics.Raycast(ray, out RaycastHit hit, MaxRaycastDistance, ~0, QueryTriggerInteraction.Ignore);

        if (m_hasValidHit)
        {
            EnsureGhost();
            UpdateGhostPose(hit);
            DrawDebugHandles(hit.point);
        }
        else if (m_ghost != null)
        {
            m_ghost.SetActive(false);
        }

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && m_hasValidHit)
        {
            PlaceInstance();
            e.Use();
        }
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            SetPlacementMode(PlacementMode.None);
            e.Use();
        }

        sceneView.Repaint();
    }

    private void EnsureGhost()
    {
        if (m_ghost != null && m_ghostSourcePrefab == m_selectedPrefab)
        {
            m_ghost.SetActive(true);
            return;
        }

        DestroyGhost();

        m_ghost = Object.Instantiate(m_selectedPrefab);
        m_ghost.hideFlags = HideFlags.HideAndDontSave;
        m_ghost.name = m_selectedPrefab.name + "_MountGhost";
        m_ghostSourcePrefab = m_selectedPrefab;
        m_ghostBaseRotation = m_ghost.transform.rotation;

        // Keep Component_MountPoint on this ghost - the mount-to-mount snap search
        // below needs it. The ship-part ghost strips it since it isn't relevant there.
        StripToVisualOnly(m_ghost, keepMountPoints: true);
    }

    private static void StripToVisualOnly(GameObject root, bool keepMountPoints)
    {
        foreach (Component c in root.GetComponentsInChildren<Component>(true))
        {
            if (c is Transform || c is MeshFilter || c is MeshRenderer || c is SkinnedMeshRenderer)
                continue;
            if (keepMountPoints && c is Component_MountPoint)
                continue;

            Object.DestroyImmediate(c);
        }
    }

    private void DestroyGhost()
    {
        if (m_ghost != null)
        {
            Object.DestroyImmediate(m_ghost);
        }
        m_ghost = null;
        m_ghostSourcePrefab = null;
        m_currentlySnapped = false;
        m_snappedSceneMount = null;
    }

    private void UpdateGhostPose(RaycastHit hit)
    {
        // Base pose: follow the mouse, align to whatever surface it's over.
        m_ghost.transform.position = hit.point;
        m_ghost.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * m_ghostBaseRotation;

        m_currentlySnapped = TryFindMountSnap(hit.point, out Vector3 snapPos, out Quaternion snapRot);
        if (m_currentlySnapped)
        {
            m_ghost.transform.position = snapPos;
            m_ghost.transform.rotation = snapRot;
        }

        ApplyGhostTint(m_ghost, m_currentlySnapped ? SnappedTint : FreeTint);
    }

    /// <summary>
    /// Finds the closest (ghost mount, scene mount) pair within m_snapRadius of
    /// searchCenter and computes the rigid transform that snaps them together
    /// (positions coincide, normals opposed), applied to the ghost's root.
    /// </summary>
    private bool TryFindMountSnap(Vector3 searchCenter, out Vector3 targetPosition, out Quaternion targetRotation)
    {
        targetPosition = default;
        targetRotation = default;
        m_snappedSceneMount = null;
        m_bestGhostMountLocalId = null;

        Component_MountPoint[] ghostMounts = m_ghost.GetComponentsInChildren<Component_MountPoint>(true);
        if (ghostMounts.Length == 0) return false;

        Component_MountPoint[] allSceneMounts = Object.FindObjectsByType<Component_MountPoint>(FindObjectsSortMode.None);

        Component_MountPoint bestGhostMount = null;
        Component_MountPoint bestSceneMount = null;
        float bestDistSqr = float.MaxValue;

        foreach (Component_MountPoint sceneMount in allSceneMounts)
        {
            if (sceneMount.transform.IsChildOf(m_ghost.transform)) continue;
            if (sceneMount.IsOccupied) continue;

            float effectiveRadius = sceneMount.SnapRadiusOverride > 0f ? sceneMount.SnapRadiusOverride : m_snapRadius;
            float radiusSqr = effectiveRadius * effectiveRadius;

            float centerDistSqr = (sceneMount.transform.position - searchCenter).sqrMagnitude;
            if (centerDistSqr > radiusSqr) continue;

            foreach (Component_MountPoint ghostMount in ghostMounts)
            {
                if (!ghostMount.CanConnectTo(sceneMount)) continue;

                float pairDistSqr = (ghostMount.transform.position - sceneMount.transform.position).sqrMagnitude;
                if (pairDistSqr < bestDistSqr)
                {
                    bestDistSqr = pairDistSqr;
                    bestGhostMount = ghostMount;
                    bestSceneMount = sceneMount;
                }
            }
        }

        if (bestGhostMount == null) return false;

        m_bestGhostMountLocalId = bestGhostMount.LocalId;

        Transform root = m_ghost.transform;
        Transform mount = bestGhostMount.transform;

        // Root-relative offset of the chosen mount, captured from the ghost's
        // current (base) pose. Only the root moves as a rigid body from here on,
        // so this offset - both the rotation and position parts - stays constant.
        Quaternion rotationOffset = Quaternion.Inverse(root.rotation) * mount.rotation;
        Vector3 positionOffsetLocal = Quaternion.Inverse(root.rotation) * (mount.position - root.position);

        // Full target orientation for the mount itself: forward opposed AND up
        // matched (not opposed). Constraining forward alone leaves the twist
        // around that axis free, which is what let the ghost flip upside down -
        // LookRotation pins both axes at once so there's no leftover freedom to flip.
        Vector3 desiredForward = -bestSceneMount.transform.forward;
        Vector3 desiredUp = bestSceneMount.transform.up;
        Quaternion mountWorldTarget = Quaternion.LookRotation(desiredForward, desiredUp);

        targetRotation = mountWorldTarget * Quaternion.Inverse(rotationOffset);

        // Recover where the mount would land under that root rotation, then
        // translate the root so the mount lines up with the scene mount exactly.
        Vector3 mountPosAfterRotation = root.position + targetRotation * positionOffsetLocal;
        targetPosition = root.position + (bestSceneMount.transform.position - mountPosAfterRotation);

        m_snappedSceneMount = bestSceneMount;
        return true;
    }

    private static void ApplyGhostTint(GameObject ghost, Color color)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor(EmissionColorId, color);
        foreach (Renderer r in ghost.GetComponentsInChildren<Renderer>(true))
        {
            r.SetPropertyBlock(block);
        }
    }

    private void DrawDebugHandles(Vector3 searchCenter)
    {
        Handles.color = new Color(1f, 1f, 0f, 0.15f);
        Handles.DrawWireDisc(searchCenter, Vector3.up, m_snapRadius);
        Handles.DrawWireDisc(searchCenter, Vector3.right, m_snapRadius);
        Handles.DrawWireDisc(searchCenter, Vector3.forward, m_snapRadius);

        if (m_currentlySnapped && m_snappedSceneMount != null)
        {
            Handles.color = Color.green;
            Handles.DrawLine(m_snappedSceneMount.transform.position,
                m_snappedSceneMount.transform.position + m_snappedSceneMount.transform.forward * 0.4f);
        }
    }

    private void PlaceInstance()
    {
        if (m_ghost == null || m_selectedPrefab == null) return;

        GameObject placed = (GameObject)PrefabUtility.InstantiatePrefab(m_selectedPrefab);
        placed.transform.SetPositionAndRotation(m_ghost.transform.position, m_ghost.transform.rotation);

        if (m_currentlySnapped && m_snappedSceneMount != null)
        {
            if (m_parentToMountTarget)
            {
                placed.transform.SetParent(m_snappedSceneMount.transform, true);
            }

            // Mark the matching mount point on the *real* instance (not the ghost,
            // which gets reused/destroyed) as connected, and occupy the scene mount
            // so the placer skips it on subsequent placements. Matched by stable ID
            // rather than array position - see Component_MountPoint.LocalId.
            if (!string.IsNullOrEmpty(m_bestGhostMountLocalId))
            {
                Component_MountPoint placedMount = null;
                foreach (Component_MountPoint candidate in placed.GetComponentsInChildren<Component_MountPoint>(true))
                {
                    if (candidate.LocalId == m_bestGhostMountLocalId)
                    {
                        placedMount = candidate;
                        break;
                    }
                }

                if (placedMount != null)
                {
                    placedMount.MarkConnected(m_snappedSceneMount);
                    m_snappedSceneMount.MarkConnected(placedMount);
                }
                else
                {
                    Debug.LogWarning("Editor_PrefabMountPlacer: couldn't find the matching mount point by ID on the placed instance - connection not marked.");
                }
            }
        }

        Undo.RegisterCreatedObjectUndo(placed, "Place " + m_selectedPrefab.name);
        Selection.activeGameObject = placed;
    }

    // ===================================================================
    // Ship part placement (right panel)
    // ===================================================================

    private void DrawShipPartPanel()
    {
        EditorGUILayout.LabelField("Ship Parts", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh List"))
        {
            RefreshShipPartList();
        }

        m_shipPartScrollPos = EditorGUILayout.BeginScrollView(m_shipPartScrollPos, "box", GUILayout.Height(180));
        if (m_availableShipParts.Count == 0)
        {
            EditorGUILayout.HelpBox("No prefabs found containing a Component_ShipPart.", MessageType.Info);
        }
        foreach (GameObject prefab in m_availableShipParts)
        {
            bool isSelected = prefab == m_selectedShipPart;
            GUI.backgroundColor = isSelected ? new Color(0.5f, 0.8f, 1f) : Color.white;
            if (GUILayout.Button(prefab.name))
            {
                m_selectedShipPart = prefab;
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        bool active = m_placementMode == PlacementMode.ShipPart;
        using (new EditorGUI.DisabledScope(m_selectedShipPart == null))
        {
            GUI.backgroundColor = active ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f);
            string label = active ? "Exit Placement Mode (Esc)" : "Enter Placement Mode";
            if (GUILayout.Button(label, GUILayout.Height(30)))
            {
                SetPlacementMode(active ? PlacementMode.None : PlacementMode.ShipPart);
            }
            GUI.backgroundColor = Color.white;
        }

        if (active)
        {
            string label = m_selectedShipPart != null ? m_selectedShipPart.name : "";
            string status = m_shipPartHoveredSurface != null
                ? "Left-click to place " + label + ", installed, on " + m_shipPartHoveredSurface.name + "."
                : "Hover a Component_BuildableSurface to place " + label + ".";
            EditorGUILayout.HelpBox(status, MessageType.Info);
        }
    }

    private void RefreshShipPartList()
    {
        m_availableShipParts.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;

            if (asset.GetComponentInChildren<Component_ShipPart>(true) != null)
            {
                m_availableShipParts.Add(asset);
            }
        }
    }

    private void OnSceneGUI_ShipPart(SceneView sceneView)
    {
        if (m_selectedShipPart == null)
        {
            return;
        }

        sceneView.wantsMouseMove = true;

        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);

        Event e = Event.current;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        // Collide (not Ignore) - Component_BuildableSurface's own runtime interaction
        // raycast may be trigger-based, so we shouldn't skip triggers here.
        m_shipPartHasValidHit = Physics.Raycast(ray, out RaycastHit hit, MaxRaycastDistance, ~0, QueryTriggerInteraction.Collide);

        m_shipPartHoveredSurface = m_shipPartHasValidHit
            ? hit.collider.GetComponentInParent<Component_BuildableSurface>()
            : null;

        if (m_shipPartHoveredSurface != null)
        {
            EnsureShipPartGhost();
            UpdateShipPartGhostPose(hit, m_shipPartHoveredSurface, sceneView.camera);
        }
        else if (m_shipPartGhost != null)
        {
            m_shipPartGhost.SetActive(false);
        }

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && m_shipPartHoveredSurface != null)
        {
            PlaceShipPartInstance(m_shipPartHoveredSurface);
            e.Use();
        }
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            SetPlacementMode(PlacementMode.None);
            e.Use();
        }

        sceneView.Repaint();
        Repaint(); // keep the HelpBox status text in the tool window live
    }

    private void EnsureShipPartGhost()
    {
        if (m_shipPartGhost != null && m_shipPartGhostSourcePrefab == m_selectedShipPart)
        {
            m_shipPartGhost.SetActive(true);
            return;
        }

        DestroyShipPartGhost();

        m_shipPartGhost = Object.Instantiate(m_selectedShipPart);
        m_shipPartGhost.hideFlags = HideFlags.HideAndDontSave;
        m_shipPartGhost.name = m_selectedShipPart.name + "_ShipPartGhost";
        m_shipPartGhostSourcePrefab = m_selectedShipPart;

        // No gameplay components on the ghost - it's a preview only, and we don't
        // want Awake() wiring up connectors/highlight state on something we're
        // about to destroy.
        StripToVisualOnly(m_shipPartGhost, keepMountPoints: false);
    }

    private void DestroyShipPartGhost()
    {
        if (m_shipPartGhost != null)
        {
            Object.DestroyImmediate(m_shipPartGhost);
        }
        m_shipPartGhost = null;
        m_shipPartGhostSourcePrefab = null;
        m_shipPartHoveredSurface = null;
    }

    /// <summary>
    /// Mirrors Component_BuildableSurface.GetGhostRotation()'s runtime logic: align
    /// to the surface's up/forward, but flip whichever axis points away from the
    /// viewer so the ghost always faces you. Runtime uses the player camera; here we
    /// use the scene view camera instead.
    /// </summary>
    private void UpdateShipPartGhostPose(RaycastHit hit, Component_BuildableSurface surface, Camera sceneCamera)
    {
        m_shipPartGhost.transform.position = hit.point;

        Vector3 surfaceUp = surface.transform.up;
        Vector3 surfaceForward = surface.transform.forward;

        Vector3 toCamera = sceneCamera != null
            ? (sceneCamera.transform.position - hit.point).normalized
            : surfaceForward;

        Vector3 resolvedUp = Vector3.Dot(toCamera, surfaceUp) < 0f ? -surfaceUp : surfaceUp;
        Vector3 resolvedForward = Vector3.Dot(toCamera, surfaceForward) < 0f ? -surfaceForward : surfaceForward;

        m_shipPartGhost.transform.rotation = Quaternion.LookRotation(resolvedForward, resolvedUp);

        ApplyGhostTint(m_shipPartGhost, SnappedTint);
    }

    private void PlaceShipPartInstance(Component_BuildableSurface surface)
    {
        if (m_shipPartGhost == null || m_selectedShipPart == null) return;

        GameObject placed = (GameObject)PrefabUtility.InstantiatePrefab(m_selectedShipPart);
        placed.transform.SetPositionAndRotation(m_shipPartGhost.transform.position, m_shipPartGhost.transform.rotation);

        Component_ShipPart shipPart = placed.GetComponentInChildren<Component_ShipPart>(true);
        if (shipPart == null)
        {
            Debug.LogWarning("Editor_PrefabMountPlacer: placed ship part prefab has no Component_ShipPart - install not applied.");
        }
        else
        {
            // Same downstream code path as a fully-completed runtime install - see
            // Component_ShipPart.PlaceAsInstalled().
            shipPart.PlaceAsInstalled(surface);
        }

        Undo.RegisterCreatedObjectUndo(placed, "Place " + m_selectedShipPart.name);
        Selection.activeGameObject = placed;
    }
}