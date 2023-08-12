#if UNITY_EDITOR

using System;
using SerializeInterface.Serializable;
using UnityEditor;
using UnityEngine;


    // [CustomEditor(typeof(SerializableTest))]
    // public class SerializableTestEditor : Editor
    // {
    //     private SerializedProperty m_TestSerializedProperty;
    //     
    //     private void OnEnable()
    //     {
    //         m_TestSerializedProperty = serializedObject.FindProperty("testListSerialized");
    //     }
    //
    //     public override void OnInspectorGUI()
    //     {
    //         serializedObject.Update();
    //
    //         DrawDefaultInspector();
    //         
    //         // Get every field in the class
    //         var fields = target.GetType().GetFields();
    //
    //         foreach (var field in fields)
    //         {
    //             var property = serializedObject.FindProperty(field.Name);
    //             if (property == null) continue;
    //             EditorGUILayout.PropertyField(property);
    //         }
    //         
    //         serializedObject.ApplyModifiedProperties();
    //     }
    // }


[CustomPropertyDrawer(typeof(DrawInterfaceAttribute))]
public class TestDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Determine if the property is expanded
        bool isExpanded = property.isExpanded;

        // Get the standard height
        float standardHeight = EditorGUI.GetPropertyHeight(property);

        label.text = property.displayName;

        // Draw the property itself
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - 20f, standardHeight), property, label, true);

        string buttonLabel = property.managedReferenceValue == null ? "+" : "x";
        if (GUI.Button(new Rect(position.x + position.width - 20f, position.y, 20f, EditorGUIUtility.singleLineHeight), buttonLabel))
        {
            if (property.managedReferenceValue == null)
            {
                property.managedReferenceValue = Activator.CreateInstance(typeof(TestA));
            }
            else
            {
                property.managedReferenceValue = null;
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}

#endif