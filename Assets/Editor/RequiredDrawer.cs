// RequiredDrawer.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RequiredAttribute))]
public class RequiredDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUI.GetPropertyHeight(property, label, true);
        if (IsEmpty(property)) height += EditorGUIUtility.singleLineHeight;
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool isEmpty = IsEmpty(property);

        if (isEmpty)
        {
            Rect warningRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.HelpBox(warningRect, $"{label.text} is required!", MessageType.Error);
            position.y += EditorGUIUtility.singleLineHeight;
            position.height -= EditorGUIUtility.singleLineHeight;
        }

        EditorGUI.PropertyField(position, property, label, true);
    }

    private bool IsEmpty(SerializedProperty property)
    {
        if (property.propertyType == SerializedPropertyType.ObjectReference)
            return property.objectReferenceValue == null;
        return false; // extend for strings, etc. if needed
    }
}
#endif