using System;
using UnityEngine.UIElements;

public static class UIUtils
{
    public static T QOrFail<T>(this VisualElement root, string name = "") where T : VisualElement
    {
        T element;
        if (name != "")
        {
            element = root.Q<T>(name);
        }
        else
        {
            element = root.Q<T>();
        }
        if (element == null)
            throw new Exception($"UI element of type {typeof(T).Name} with name \"{name}\" not found.");
        return element;
    }
}

