using System;
using UnityEngine;
namespace Ommy.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class InspectorButtonAttribute : PropertyAttribute
    {
        public string ButtonLabel { get; }

        public InspectorButtonAttribute(string buttonLabel = null)
        {
            ButtonLabel = buttonLabel;
        }
    }
    /// <summary>
    /// Displays a property or field value in the Inspector as a read-only field.
    /// Usage: [ShowProperty("propertyName")] or [ShowProperty("otherObject.propertyName")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class ShowPropertyAttribute : PropertyAttribute
    {
        public string PropertyPath { get; }
        public string Label { get; }

        public ShowPropertyAttribute(string propertyPath, string label = null)
        {
            PropertyPath = propertyPath;
            Label = label;
        }
    }

    /// <summary>
    /// Shows a property or field with getter in the Inspector as read-only.
    /// Apply directly to properties: [ShowInInspector] public int MyProperty => value;
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
    public class ShowInInspectorAttribute : Attribute
    {
        public string Label { get; }

        public ShowInInspectorAttribute(string label = null)
        {
            Label = label;
        }
    }
}