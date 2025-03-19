using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;
using System.Collections.Generic;
using Ommy.Attributes;
[CustomEditor(typeof(MonoBehaviour), true)]
public class InspectorButtonDrawer : Editor
{
    private class MethodData
    {
        public MethodInfo Method;
        public object[] Parameters;
    }

    private Dictionary<MethodInfo, object[]> _methodParameters = new Dictionary<MethodInfo, object[]>();

    public override void OnInspectorGUI()
    {
        // Draw default Inspector UI
        DrawDefaultInspector();

        // Get the target MonoBehaviour
        MonoBehaviour targetMonoBehaviour = (MonoBehaviour)target;

        // Get all methods of the target script
        MethodInfo[] methods = targetMonoBehaviour.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            // Check if the method has the InspectorButtonAttribute
            var attribute = method.GetCustomAttribute<InspectorButtonAttribute>();
            if (attribute == null) continue;

            string buttonLabel = attribute.ButtonLabel ?? method.Name;

            // Check and display parameters
            ParameterInfo[] parameters = method.GetParameters();
            if (!_methodParameters.ContainsKey(method))
            {
                _methodParameters[method] = new object[parameters.Length];
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo param = parameters[i];
                _methodParameters[method][i] = DrawParameterField(param, _methodParameters[method][i]);
            }

            // Draw the button
            if (GUILayout.Button(buttonLabel))
            {
                try
                {
                    method.Invoke(targetMonoBehaviour, _methodParameters[method]);
                }
                catch (TargetParameterCountException)
                {
                    Debug.LogError($"Parameter mismatch when invoking method '{method.Name}' on {targetMonoBehaviour.name}.");
                }
            }
        }
    }

    private object DrawParameterField(ParameterInfo parameter, object currentValue)
    {
        object value = currentValue;
        System.Type paramType = parameter.ParameterType;

        GUILayout.BeginHorizontal();
        GUILayout.Label(parameter.Name);

        if (paramType == typeof(int))
        {
            value = EditorGUILayout.IntField(value != null ? (int)value : 0);
        }
        else if (paramType == typeof(float))
        {
            value = EditorGUILayout.FloatField(value != null ? (float)value : 0f);
        }
        else if (paramType == typeof(string))
        {
            value = EditorGUILayout.TextField(value != null ? (string)value : "");
        }
        else if (paramType == typeof(bool))
        {
            value = EditorGUILayout.Toggle(value != null && (bool)value);
        }
        else if (paramType.IsEnum)
        {
            value = EditorGUILayout.EnumPopup(value != null ? (Enum)value : (Enum)Activator.CreateInstance(paramType));
        }
        else if (paramType == typeof(Vector2))
        {
            value = EditorGUILayout.Vector2Field("", value != null ? (Vector2)value : Vector2.zero);
        }
        else if (paramType == typeof(Vector3))
        {
            value = EditorGUILayout.Vector3Field("", value != null ? (Vector3)value : Vector3.zero);
        }
        else if (paramType == typeof(GameObject))
        {
            value = EditorGUILayout.ObjectField(value as GameObject, typeof(GameObject), true);
        }
        else if (paramType == typeof(Transform))
        {
            value = EditorGUILayout.ObjectField(value as Transform, typeof(Transform), true);
        }
        else if (paramType == typeof(Color))
        {
            value = EditorGUILayout.ColorField(value != null ? (Color)value : Color.white);
        }
        else
        {
            EditorGUILayout.LabelField($"Unsupported: {paramType.Name}");
        }

        GUILayout.EndHorizontal();

        return value;
    }
}
