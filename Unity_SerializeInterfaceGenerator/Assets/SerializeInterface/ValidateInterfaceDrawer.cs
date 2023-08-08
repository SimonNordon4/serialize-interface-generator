#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace SerializeInterface
{
    [CustomPropertyDrawer(typeof(ValidateInterfaceAttribute))]
    public class ValidateInterfaceDrawer : PropertyDrawer
    {
        private string GetGenericTypeName(Type type)
        {
            if (!type.IsGenericType)
                return GetFriendlyTypeName(type);

            string genericTypeName = type.GetGenericTypeDefinition().Name;
            // Removing the "`1" part
            genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));

            string genericArgs = string.Join(",",
                type.GetGenericArguments()
                    .Select(t => GetGenericTypeName(t)).ToArray()); // Recursive call for nested generics

            return $"{genericTypeName}<{genericArgs}>";
        }

        private string GetFriendlyTypeName(Type type)
        {
            switch (type.Name)
            {
                case "Int32":
                    return "int";
                case "Boolean":
                    return "bool";
                case "String":
                    return "string";
                case "Double":
                    return "double";
                case "Single":
                    return "float";
                case "Int64":
                    return "long";
                case "Int16":
                    return "short";
                case "Byte":
                    return "byte";
                case "Char":
                    return "char";
                case "Decimal":
                    return "decimal";
                // Add more types as needed
                default:
                    return type.Name;
            }
        }



        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get the attribute
            ValidateInterfaceAttribute interfaceAttribute = (ValidateInterfaceAttribute)attribute;

            // remove the last 10 characters from the label
            //label.text = label.text.Substring(0, label.text.Length - 10);
            var defaultText = label.text;

            var typePrettyName = GetGenericTypeName(interfaceAttribute.RequiredType);

            if (defaultText.Contains("Element"))
                label.text = $"{typePrettyName}";
                
            if(defaultText.Contains("Serialized"))
                label.text = $"{label.text.Substring(0, label.text.Length - 10)} ({typePrettyName})";

            // Draw the property
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue,
                typeof(UnityEngine.Object), true);

            if (EditorGUI.EndChangeCheck())
            {
                if (newValue == null)
                {
                    property.objectReferenceValue = null;
                }
                else if (newValue is GameObject gameObject)
                {
                    // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                    var component = gameObject.GetComponent(interfaceAttribute.RequiredType);
                    // ReSharper disable once Unity.PerformanceCriticalCodeNullComparison
                    if (component != null)
                    {
                        property.objectReferenceValue = component;
                    }
                    else
                    {
                        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                        Debug.LogWarning(
                            $"The assigned GameObject does not contain a component that implements the required interface {interfaceAttribute.RequiredType.Name}");
                    }
                }
                else if ((newValue is MonoBehaviour || newValue is ScriptableObject) &&
                         interfaceAttribute.RequiredType.IsInstanceOfType(newValue))
                {
                    property.objectReferenceValue = newValue;
                }
                else
                {
                    Debug.LogWarning(
                        $"The assigned object does not implement the required interface {interfaceAttribute.RequiredType.Name}");
                }
            }
        }
    }
}
#endif