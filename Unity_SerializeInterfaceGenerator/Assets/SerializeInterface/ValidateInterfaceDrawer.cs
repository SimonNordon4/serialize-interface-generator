#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace SerializeInterface
{
    [CustomPropertyDrawer(typeof(ValidateInterfaceAttribute))]
    public class ValidateInterfaceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get the attribute
            ValidateInterfaceAttribute interfaceAttribute = (ValidateInterfaceAttribute)attribute;

            // Set the label to the name of the required interface
            label.text = interfaceAttribute.RequiredType.Name;

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
                    var component = gameObject.GetComponent(interfaceAttribute.RequiredType);
                    if (component != null)
                    {
                        property.objectReferenceValue = component;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"The assigned GameObject does not contain a component that implements the required interface {interfaceAttribute.RequiredType.Name}");
                    }
                }
                else if ((newValue is MonoBehaviour || newValue is ScriptableObject) &&
                         interfaceAttribute.RequiredType.IsAssignableFrom(newValue.GetType()))
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