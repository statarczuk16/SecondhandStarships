#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Reflection;
using UnityEngine;

public class RequiredFieldValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        foreach (var mb in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
        {
            var fields = mb.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<RequiredAttribute>() != null &&
                    field.GetValue(mb) == null)
                {
                    throw new BuildFailedException($"Required field '{field.Name}' on '{mb.name}' ({mb.GetType().Name}) is not assigned.");
                }
            }
        }
    }
}
#endif