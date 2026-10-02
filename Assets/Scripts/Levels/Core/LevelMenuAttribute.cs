using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Where a module or action appears in the Inspector's Add menu, e.g. "Hazards/Start Fire".</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LevelMenuAttribute : Attribute
{
    public string Path { get; }

    public LevelMenuAttribute(string path) => Path = path;

    public static string MenuPathOf(Type type)
    {
        var menu = (LevelMenuAttribute)GetCustomAttribute(type, typeof(LevelMenuAttribute));
        return menu != null ? menu.Path : NiceName(type);
    }

    public static string NameOf(Type type)
    {
        string path = MenuPathOf(type);
        int slash = path.LastIndexOf('/');
        return slash >= 0 ? path.Substring(slash + 1) : path;
    }

    static string NiceName(Type type)
    {
        string name = type.Name;
        foreach (string suffix in new[] { "Module", "Action" })
        {
            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
                name = name.Substring(0, name.Length - suffix.Length);
        }

        return Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");
    }
}

/// <summary>Draws a [SerializeReference] field or list with a dropdown of every concrete subclass.</summary>
public sealed class SubclassPickerAttribute : PropertyAttribute
{
}

/// <summary>Lets the Inspector title a [SerializeReference] entry with something better than "Element 0".</summary>
public interface IInspectorLabel
{
    string Label { get; }
}
