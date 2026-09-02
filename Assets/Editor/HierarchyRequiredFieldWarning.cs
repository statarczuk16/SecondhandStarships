// HierarchyRequiredFieldWarning.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

[InitializeOnLoad]
public static class HierarchyRequiredFieldWarning
{
    private static readonly Dictionary<EntityId, bool> issueCache = new Dictionary<EntityId, bool>();
    private static double lastRefreshTime;
    private const double RefreshInterval = 0.5; // seconds

    static HierarchyRequiredFieldWarning()
    {
        EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyItemGUI;
        EditorApplication.hierarchyChanged += ClearCache;
        Undo.undoRedoPerformed += ClearCache;
        EditorApplication.update += ThrottledRefreshCheck;
    }

    private static void ThrottledRefreshCheck()
    {
        if (EditorApplication.timeSinceStartup - lastRefreshTime > RefreshInterval)
        {
            ClearCache();
            lastRefreshTime = EditorApplication.timeSinceStartup;
        }
    }

    private static void ClearCache() => issueCache.Clear();

    private static void OnHierarchyItemGUI(EntityId entityId, Rect selectionRect)
    {
        GameObject go = EditorUtility.EntityIdToObject(entityId) as GameObject;
        if (go == null) return;

        if (HasIssueInSubtree(go))
        {
            Rect iconRect = new Rect(selectionRect.xMax - 18f, selectionRect.y, 16f, selectionRect.height);
            GUI.Label(iconRect, EditorGUIUtility.IconContent("console.erroricon.sml"));

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && iconRect.Contains(Event.current.mousePosition))
            {
                GameObject culprit = FindFirstIssueObject(go);
                if (culprit != null)
                {
                    Selection.activeGameObject = culprit;
                    EditorGUIUtility.PingObject(culprit);
                }

                Event.current.Use();
            }
        }
    }

    private static bool HasIssueInSubtree(GameObject go)
    {
        EntityId id = go.GetEntityId();
        if (issueCache.TryGetValue(id, out bool cached))
            return cached;

        bool hasIssue = HasMissingRequiredField(go);

        if (!hasIssue)
        {
            foreach (Transform child in go.transform)
            {
                if (HasIssueInSubtree(child.gameObject))
                {
                    hasIssue = true;
                    break;
                }
            }
        }

        issueCache[id] = hasIssue;
        return hasIssue;
    }

    private static GameObject FindFirstIssueObject(GameObject go)
    {
        if (HasMissingRequiredField(go))
            return go;

        foreach (Transform child in go.transform)
        {
            GameObject found = FindFirstIssueObject(child.gameObject);
            if (found != null)
                return found;
        }

        return null;
    }

    private static bool HasMissingRequiredField(GameObject go)
    {
        var components = go.GetComponents<MonoBehaviour>();
        foreach (var mb in components)
        {
            if (mb == null) continue;
            var fields = mb.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<RequiredAttribute>() != null && field.GetValue(mb) == null)
                    return true;
            }
        }
        return false;
    }
}
#endif