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
}