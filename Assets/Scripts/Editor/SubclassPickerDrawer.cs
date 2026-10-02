using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SubclassPickerAttribute))]
public class SubclassPickerDrawer : PropertyDrawer
{
    static readonly Dictionary<Type, Type[]> ConcreteTypesByBase = new();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
        property.propertyType == SerializedPropertyType.ManagedReference
            ? EditorGUI.GetPropertyHeight(property, true)
            : EditorGUIUtility.singleLineHeight;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.LabelField(position, label.text, "[SubclassPicker] needs [SerializeReference]");
            return;
        }

        object value = property.managedReferenceValue;
        string title = value switch
        {
            null => "(empty)",
            IInspectorLabel labelled => labelled.Label,
            _ => ObjectNames.NicifyVariableName(value.GetType().Name),
        };
        var content = new GUIContent(title, label.tooltip);

        float buttonWidth = Mathf.Min(140f, position.width * 0.4f);
        var buttonRect = new Rect(position.xMax - buttonWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);

        EditorGUI.BeginProperty(position, content, property);

        // Drawn before the field so the button gets the click instead of the foldout row under it.
        string buttonText = value == null ? "Choose type..." : LevelMenuAttribute.NameOf(value.GetType());
        if (EditorGUI.DropdownButton(buttonRect, new GUIContent(buttonText), FocusType.Keyboard))
            ShowTypeMenu(property);

        EditorGUI.PropertyField(position, property, content, true);
        EditorGUI.EndProperty();
    }

    static void ShowTypeMenu(SerializedProperty property)
    {
        SerializedObject owner = property.serializedObject;
        string path = property.propertyPath;
        Type current = property.managedReferenceValue?.GetType();

        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("(Empty)"), current == null, () => Assign(owner, path, null));
        menu.AddSeparator("");
        foreach (Type type in ConcreteTypes(BaseTypeOf(property)))
        {
            Type choice = type;
            menu.AddItem(new GUIContent(LevelMenuAttribute.MenuPathOf(type)), current == type, () => Assign(owner, path, choice));
        }

        menu.ShowAsContext();
    }

    // The menu callback runs later, after this frame's SerializedProperty may be stale, so look it up again.
    static void Assign(SerializedObject owner, string path, Type type)
    {
        owner.Update();
        SerializedProperty property = owner.FindProperty(path);
        if (property == null)
            return;

        property.managedReferenceValue = type != null ? Activator.CreateInstance(type) : null;
        owner.ApplyModifiedProperties();
    }

    static Type BaseTypeOf(SerializedProperty property)
    {
        // Unity reports it as "AssemblyName Namespace.TypeName".
        string[] parts = property.managedReferenceFieldTypename.Split(' ');
        Type type = parts.Length == 2 ? Type.GetType($"{parts[1]}, {parts[0]}") : null;
        return type ?? typeof(object);
    }

    static Type[] ConcreteTypes(Type baseType)
    {
        if (ConcreteTypesByBase.TryGetValue(baseType, out Type[] cached))
            return cached;

        Type[] types = TypeCache.GetTypesDerivedFrom(baseType)
            .Where(t => !t.IsAbstract && !t.IsGenericType
                        && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                        && t.IsDefined(typeof(SerializableAttribute), false)
                        && t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(LevelMenuAttribute.MenuPathOf)
            .ToArray();

        ConcreteTypesByBase[baseType] = types;
        return types;
    }
}
